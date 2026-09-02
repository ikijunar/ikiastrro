-- =====================================================================
-- ikiastrro - consolidated database baseline
-- Generated 2026-08-30 from the live schema of the former 'vedic_horo_gen' database
-- (SMO script-out). Replaces the db/001..034 migration chain as the single
-- from-scratch build. Schema for all tables/views/functions + seed data for the
-- reference/master tables (Planets, SignAttributes, Nakshatras + Padas + SubLords,
-- PlanetSignTransitEvents, Rule_*, Dim_LagnaFunctionalNature, Dim_Source) + the LifeCalendar
-- dimension. Per-person tables (BirthDetails, ChartResults, Chart_*, DashaPeriods)
-- are schema-only - the app fills them.
-- =====================================================================

-- Catalog name is a sqlcmd scripting variable so this one file builds any environment
-- (dev = ikiastrro, a scratch check = ikiastrro_scratch). REQUIRES SQLCMD MODE: the `sqlcmd`
-- CLI has it on by default; in SSMS, Query > SQLCMD Mode. Override:  sqlcmd -v DbName=<name>
:setvar DbName "ikiastrro"
GO
IF DB_ID(N'$(DbName)') IS NULL CREATE DATABASE [$(DbName)];
GO
USE [$(DbName)];
GO

-- Schema-migration ledger (folded from db/01_create_schema_migrations.sql).
-- The baseline creates the table only; numbered migration scripts record
-- themselves via INSERT when applied to an existing database.
IF OBJECT_ID('dbo.SchemaMigrations', 'U') IS NULL
CREATE TABLE dbo.SchemaMigrations (
    ScriptName    VARCHAR(120)  NOT NULL CONSTRAINT PK_SchemaMigrations PRIMARY KEY,
    AppliedAtUtc  DATETIME2(0)  NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedAtUtc DEFAULT sysutcdatetime(),
    ScriptHash    CHAR(64)      NULL,
    Note          VARCHAR(200)  NULL
);
GO

-- ------------------------- SCHEMA -------------------------
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fn_GetNakshatraRulingPlanetId]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
execute dbo.sp_executesql @statement = N'
CREATE FUNCTION [dbo].[fn_GetNakshatraRulingPlanetId] (@NakshatraId TINYINT)
RETURNS TINYINT
AS
BEGIN
    DECLARE @PlanetId TINYINT;
    SELECT @PlanetId = RulingPlanetId FROM dbo.tbl_Nakshatras WHERE Id = @NakshatraId;
    RETURN @PlanetId;
END
' 
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Planets]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Planets](
	[Id] [tinyint] NOT NULL,
	[PlanetName] [varchar](15) NOT NULL,
	[PlanetNameSanskrit] [varchar](15) NOT NULL,
	[NaturalNature] [varchar](12) NOT NULL,
	[ConditionalRule] [varchar](100) NULL,
	[RulesSign] [bit] NOT NULL,
	[VimshottariYears] [tinyint] NOT NULL,
	[VimshottariSequenceOrder] [tinyint] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[VimshottariSequenceOrder] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[PlanetName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_SignAttributes]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_SignAttributes](
	[Id] [tinyint] NOT NULL,
	[SignName] [varchar](20) NOT NULL,
	[SignNameSanskrit] [varchar](20) NOT NULL,
	[ZodiacEnumValue] [varchar](20) NOT NULL,
	[RulingPlanetId] [tinyint] NOT NULL,
	[type_house_element] [varchar](10) NOT NULL,
	[type_house_keyattri] [varchar](15) NOT NULL,
	[Gender] [varchar](10) NOT NULL,
	[Direction] [varchar](10) NOT NULL,
	[RisingType] [varchar](15) NULL,
	[SymbolAnimalType] [varchar](20) NOT NULL,
	[SymbolDescription] [varchar](50) NOT NULL,
	[KalapurushaBodyPart] [varchar](30) NOT NULL,
	[ExaltedPlanetId] [tinyint] NULL,
	[ExaltedDegree] [decimal](5, 2) NULL,
	[DebilitatedPlanetId] [tinyint] NULL,
	[DebilitatedDegree] [decimal](5, 2) NULL,
	[MooltrikonaPlanetId] [tinyint] NULL,
	[MooltrikonaRangeStart] [decimal](5, 2) NULL,
	[MooltrikonaRangeEnd] [decimal](5, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[ZodiacEnumValue] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[SignName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Nakshatras]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Nakshatras](
	[Id] [tinyint] NOT NULL,
	[NakshatraName] [varchar](20) NOT NULL,
	[StartDegree] [decimal](9, 6) NOT NULL,
	[EndDegree] [decimal](9, 6) NOT NULL,
	[RulingPlanetId] [tinyint] NOT NULL,
	[SequenceNumber] [tinyint] NOT NULL,
	[RulingDeity] [varchar](30) NULL,
	[Symbol] [varchar](40) NULL,
	[Guna] [varchar](10) NULL,
	[Gana] [varchar](10) NULL,
	[YoniAnimal] [varchar](15) NULL,
	[YoniGender] [varchar](6) NULL,
	[Nadi] [varchar](6) NULL,
	[Varna] [varchar](12) NULL,
	[Tatva] [varchar](10) NULL,
	[Direction] [varchar](10) NULL,
	[PrimaryRasiId] [tinyint] NOT NULL,
	[StraddlesSignBoundary] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[SequenceNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[NakshatraName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_NakshatraPadas]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_NakshatraPadas](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[NakshatraId] [tinyint] NOT NULL,
	[PadaNumber] [tinyint] NOT NULL,
	[StartDegree] [decimal](9, 6) NOT NULL,
	[EndDegree] [decimal](9, 6) NOT NULL,
	[RasiId] [tinyint] NOT NULL,
	[NavamsaSignId] [tinyint] NOT NULL,
	[RulingPlanetId]  AS ([dbo].[fn_GetNakshatraRulingPlanetId]([NakshatraId])),
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_NakshatraPadas_Nakshatra_Pada] UNIQUE NONCLUSTERED 
(
	[NakshatraId] ASC,
	[PadaNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_HouseLords]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Chart_HouseLords](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ChartResultId] [int] NOT NULL,
	[HouseNumber] [tinyint] NOT NULL,
	[HouseSign] [varchar](20) NOT NULL,
	[LordPlanet] [varchar](20) NOT NULL,
	[LordPlacedInHouseFromLagna] [tinyint] NOT NULL,
	[LordPlacedInHouseFromSun] [tinyint] NOT NULL,
	[LordPlacedInHouseFromMoon] [tinyint] NOT NULL,
	[LordPlacedInSign] [varchar](20) NOT NULL,
	[LordDignityStatus] [varchar](20) NULL,
	[HouseSignId] [tinyint] NOT NULL,
	[LordPlanetId] [tinyint] NOT NULL,
	[LordPlacedInSignId] [tinyint] NOT NULL,
 CONSTRAINT [FK_HouseLords_HouseSign]  FOREIGN KEY ([HouseSignId])        REFERENCES [dbo].[tbl_SignAttributes] ([Id]),
 CONSTRAINT [FK_HouseLords_Lord]       FOREIGN KEY ([LordPlanetId])       REFERENCES [dbo].[tbl_Planets] ([Id]),
 CONSTRAINT [FK_HouseLords_LordInSign] FOREIGN KEY ([LordPlacedInSignId]) REFERENCES [dbo].[tbl_SignAttributes] ([Id]),
 CONSTRAINT [CK_HouseLords_House]      CHECK ([HouseNumber] BETWEEN 1 AND 12),
 CONSTRAINT [CK_HouseLords_LordHouses] CHECK ([LordPlacedInHouseFromLagna] BETWEEN 1 AND 12 AND [LordPlacedInHouseFromSun] BETWEEN 1 AND 12 AND [LordPlacedInHouseFromMoon] BETWEEN 1 AND 12),
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
-- vw_Chart_HouseNakshatraSpan is defined near the end of this file (just
-- before vw_Chart_Consolidated): after migration 09 it joins tbl_ChartResults
-- and tbl_Dim_ChartType, both of which are created later in this script.
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_DashaPeriods]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Chart_DashaPeriods](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ChartResultId] [int] NOT NULL,
	[ParentDashaPeriodId] [int] NULL,
	[LevelNumber] [tinyint] NOT NULL,
	[SequenceInParent] [tinyint] NOT NULL,
	[Lord] [varchar](20) NOT NULL,
	[StartDate] [datetime2](0) NOT NULL,
	[EndDate] [datetime2](0) NOT NULL,
	[StartDayOffset] [int] NOT NULL,
	[EndDayOffset] [int] NOT NULL,
	[LordId] [tinyint] NOT NULL,
	[ParentChartResultId] AS [ChartResultId] PERSISTED,
 CONSTRAINT [FK_DashaPeriods_Lord]    FOREIGN KEY ([LordId]) REFERENCES [dbo].[tbl_Planets] ([Id]),
 CONSTRAINT [CK_DashaPeriods_Dates]   CHECK ([StartDate] < [EndDate]),
 CONSTRAINT [CK_DashaPeriods_Offsets] CHECK ([StartDayOffset] <= [EndDayOffset]),
 CONSTRAINT [CK_DashaPeriods_Level]   CHECK ([LevelNumber] BETWEEN 1 AND 3),
 CONSTRAINT [UQ_DashaPeriods_Result_Id] UNIQUE ([ChartResultId], [Id]),
 CONSTRAINT [FK_DashaPeriods_ParentSameChart] FOREIGN KEY ([ParentChartResultId], [ParentDashaPeriodId]) REFERENCES [dbo].[tbl_Chart_DashaPeriods] ([ChartResultId], [Id]),
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_BirthDetails]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_BirthDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[DateOfBirth] [date] NOT NULL,
	[TimeOfBirth] [time](0) NOT NULL,
	[PlaceCity] [nvarchar](200) NOT NULL,
	[PlaceCountry] [nvarchar](200) NOT NULL,
	[Latitude] [decimal](9, 6) NOT NULL,
	[Longitude] [decimal](9, 6) NOT NULL,
	[UtcOffset] [varchar](10) NOT NULL,
	[IanaTimeZoneId] [varchar](100) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [CK_BirthDetails_LatLong] CHECK ([Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180),
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_ChartResults]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_ChartResults](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BirthDetailId] [int] NOT NULL,
	[ChartType] [nvarchar](50) NOT NULL,
	[Ayanamsha] [nvarchar](50) NOT NULL,
	[HouseSystem] [nvarchar](50) NOT NULL,
	[EngineVersion] [nvarchar](100) NOT NULL,
	[ResultJson] [nvarchar](max) NOT NULL,
	[ComputedAt] [datetime2](7) NOT NULL,
	[RuleSetId] [tinyint] NOT NULL,
	[ChartTypeId] [tinyint] NULL,
	[CalculationKind] [varchar](20) NOT NULL CONSTRAINT [DF_ChartResults_CalcKind] DEFAULT ('PositionChart'),
	[VargaMethod] [varchar](40) NULL,
	[AyanamshaDegrees] [decimal](9, 6) NULL,
	[SiderealTimeHours] [decimal](9, 6) NULL,
 CONSTRAINT [CK_ChartResults_KindType] CHECK (
        ([CalculationKind] = 'PositionChart' AND [ChartTypeId] IS NOT NULL) OR
        ([CalculationKind] <> 'PositionChart' AND [ChartTypeId] IS NULL)),
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_Chart_DashaTimeline]'))
EXEC dbo.sp_executesql @statement = N'CREATE VIEW [dbo].[vw_Chart_DashaTimeline] AS
WITH PeriodPath AS (
    SELECT
        dp.Id, dp.Lord, dp.LevelNumber,
        CAST(dp.Lord AS NVARCHAR(200)) AS PathLabel
    FROM dbo.tbl_Chart_DashaPeriods dp
    WHERE dp.ParentDashaPeriodId IS NULL
    UNION ALL
    SELECT
        dp.Id, dp.Lord, dp.LevelNumber,
        CAST(pp.PathLabel + '' > '' + dp.Lord AS NVARCHAR(200))
    FROM dbo.tbl_Chart_DashaPeriods dp
    JOIN PeriodPath pp ON pp.Id = dp.ParentDashaPeriodId
)
SELECT
    bd.Id                   AS BirthDetailId,
    bd.Name,
    cr.Id                   AS ChartResultId,
    dp.Id                   AS DashaPeriodId,
    dp.ParentDashaPeriodId,
    dp.LevelNumber,
    CASE dp.LevelNumber WHEN 1 THEN ''Mahadasha'' WHEN 2 THEN ''Antardasha'' ELSE ''Pratyantardasha'' END AS LevelName,
    dp.SequenceInParent,
    dp.Lord,
    pp.PathLabel,
    dp.StartDate,
    dp.EndDate,
    dp.StartDayOffset,
    dp.EndDayOffset
FROM dbo.tbl_Chart_DashaPeriods dp
JOIN dbo.tbl_ChartResults cr ON cr.Id = dp.ChartResultId
JOIN dbo.tbl_BirthDetails bd ON bd.Id = cr.BirthDetailId
JOIN PeriodPath pp           ON pp.Id = dp.Id;
' 
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Dim_LifeCalendar]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Dim_LifeCalendar](
	[DayOffset] [int] NOT NULL,
	[WeekNumber] [int] NOT NULL,
	[WeekStartOffset] [int] NOT NULL,
	[WeekEndOffset] [int] NOT NULL,
	[MonthNumber] [int] NOT NULL,
	[MonthStartOffset] [int] NOT NULL,
	[MonthEndOffset] [int] NOT NULL,
	[YearNumber] [int] NOT NULL,
	[YearStartOffset] [int] NOT NULL,
	[YearEndOffset] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[DayOffset] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tvf_Chart_LifeWeeks]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
execute dbo.sp_executesql @statement = N'-- Resolves the person''s own VimshottariDasha ChartResultId first (most recent, if somehow more
-- than one exists) and scopes every period join to that specific ChartResultId â€” not just
-- BirthDetailId â€” so a stale, un-deleted prior computation (e.g. after a birth-time correction
-- was recomputed without clearing the old periods first) can never silently double up rows here.
-- OUTER APPLY (not CROSS APPLY) so a person with no Dasha computed yet still returns the full
-- 4000-week grid with NULL lords, instead of an empty result.
CREATE FUNCTION [dbo].[tvf_Chart_LifeWeeks] (@BirthDetailId INT)
RETURNS TABLE
AS
RETURN
(
    SELECT
        lc.WeekNumber,
        DATEADD(DAY, lc.WeekStartOffset, bd.DateOfBirth) AS WeekStartDate,
        DATEADD(DAY, lc.WeekEndOffset,   bd.DateOfBirth) AS WeekEndDate,
        maha.Lord       AS MahaLord,
        antar.Lord      AS AntarLord,
        pratyantar.Lord AS PratyantarLord
    FROM dbo.tbl_BirthDetails bd
    OUTER APPLY (
        SELECT TOP 1 cr.Id
        FROM dbo.tbl_ChartResults cr
        WHERE cr.BirthDetailId = bd.Id AND cr.CalculationKind = ''VimshottariDasha''
        ORDER BY cr.Id DESC
    ) dashaChart(ChartResultId)
    CROSS JOIN dbo.tbl_Dim_LifeCalendar lc
    LEFT JOIN dbo.tbl_Chart_DashaPeriods maha
        ON maha.ChartResultId = dashaChart.ChartResultId AND maha.LevelNumber = 1
        AND lc.WeekStartOffset BETWEEN maha.StartDayOffset AND maha.EndDayOffset
    LEFT JOIN dbo.tbl_Chart_DashaPeriods antar
        ON antar.ChartResultId = dashaChart.ChartResultId AND antar.LevelNumber = 2
        AND lc.WeekStartOffset BETWEEN antar.StartDayOffset AND antar.EndDayOffset
    LEFT JOIN dbo.tbl_Chart_DashaPeriods pratyantar
        ON pratyantar.ChartResultId = dashaChart.ChartResultId AND pratyantar.LevelNumber = 3
        AND lc.WeekStartOffset BETWEEN pratyantar.StartDayOffset AND pratyantar.EndDayOffset
    WHERE bd.Id = @BirthDetailId
      AND lc.WeekNumber BETWEEN 1 AND 4000
      AND lc.DayOffset = lc.WeekStartOffset   -- one row per week, not one per day
);
' 
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- tbl_Chart_KeyDetails — one row per chart point per chart type. PointKind discriminates
-- 'Graha' (Ascendant + 9 planets) from the special points 'SpecialLagna' (HL) / 'Arudha'
-- (AL, A2..A12) / 'Upagraha' (Gulika, Maandi); non-'Graha' rows are position-only, enforced
-- by CK_KeyDetails_NonGrahaNulls. CharaKaraka carries the Jaimini 8-karaka label on grahas.
-- PointKind + CharaKaraka folded from db/14_add_karaka_and_pointkind.sql.
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_KeyDetails]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Chart_KeyDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ChartResultId] [int] NOT NULL,
	[Planet] [varchar](20) NOT NULL,
	[NirayanaLongitudeDegrees] [float] NOT NULL,
	[VargaLongitudeDegrees] [decimal](9, 6) NOT NULL,
	[PointKind] [varchar](12) NOT NULL CONSTRAINT DF_KeyDetails_PointKind DEFAULT ('Graha'),
	[CharaKaraka] [varchar](4) NULL,
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
	[PlanetId] [tinyint] NULL,
	[SignId] [tinyint] NOT NULL,
	[NakshatraLordPlanetId] [tinyint] NULL,
	[NakshatraSubLordPlanetId] [tinyint] NULL,
	[SignLordPlanetId] [tinyint] NULL,
 CONSTRAINT [FK_KeyDetails_Planet]     FOREIGN KEY ([PlanetId])                 REFERENCES [dbo].[tbl_Planets] ([Id]),
 CONSTRAINT [FK_KeyDetails_Sign]       FOREIGN KEY ([SignId])                   REFERENCES [dbo].[tbl_SignAttributes] ([Id]),
 CONSTRAINT [FK_KeyDetails_NakLord]    FOREIGN KEY ([NakshatraLordPlanetId])    REFERENCES [dbo].[tbl_Planets] ([Id]),
 CONSTRAINT [FK_KeyDetails_NakSubLord] FOREIGN KEY ([NakshatraSubLordPlanetId]) REFERENCES [dbo].[tbl_Planets] ([Id]),
 CONSTRAINT [FK_KeyDetails_SignLord]   FOREIGN KEY ([SignLordPlanetId])         REFERENCES [dbo].[tbl_Planets] ([Id]),
 CONSTRAINT [CK_KeyDetails_Longitude]  CHECK ([NirayanaLongitudeDegrees] >= 0 AND [NirayanaLongitudeDegrees] < 360),
 CONSTRAINT [CK_KeyDetails_VargaLongitude] CHECK ([VargaLongitudeDegrees] >= 0 AND [VargaLongitudeDegrees] < 360),
 CONSTRAINT [CK_KeyDetails_PointKind]     CHECK ([PointKind] IN ('Graha','SpecialLagna','Arudha','Upagraha')),
 CONSTRAINT [CK_KeyDetails_CharaKaraka]   CHECK ([CharaKaraka] IS NULL OR [CharaKaraka] IN ('AK','AmK','BK','MK','PiK','PK','GK','DK')),
 CONSTRAINT [CK_KeyDetails_NonGrahaNulls] CHECK ([PointKind] = 'Graha' OR ([PlanetId] IS NULL AND [DignityStatus] IS NULL AND [Nakshatra] IS NULL AND [CharaKaraka] IS NULL AND [AspectingPlanets] IS NULL AND [IsCombust] IS NULL AND [NakshatraLordPlanet] IS NULL)),
 CONSTRAINT [CK_KeyDetails_DegInSign]  CHECK ([DegreesInSignDecimal] IS NULL OR ([DegreesInSignDecimal] >= 0 AND [DegreesInSignDecimal] < 30)),
 CONSTRAINT [CK_KeyDetails_HouseLagna] CHECK ([HouseNumberFromLagna] BETWEEN 1 AND 12),
 CONSTRAINT [CK_KeyDetails_HouseSun]   CHECK ([HouseNumberFromSun] BETWEEN 1 AND 12),
 CONSTRAINT [CK_KeyDetails_HouseMoon]  CHECK ([HouseNumberFromMoon] BETWEEN 1 AND 12),
 CONSTRAINT [CK_KeyDetails_Pada]       CHECK ([NakshatraPada] IS NULL OR [NakshatraPada] BETWEEN 1 AND 4),
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_Conjunctions]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Chart_Conjunctions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ChartResultId] [int] NOT NULL,
	[Planet1] [varchar](20) NOT NULL,
	[Planet2] [varchar](20) NOT NULL,
	[Sign] [varchar](20) NOT NULL,
	[HouseNumberFromLagna] [tinyint] NOT NULL,
	[DegreeSeparation] [decimal](7, 4) NULL,
	[Planet1Id] [tinyint] NOT NULL,
	[Planet2Id] [tinyint] NOT NULL,
	[SignId] [tinyint] NOT NULL,
 CONSTRAINT [FK_Conjunctions_Planet1] FOREIGN KEY ([Planet1Id]) REFERENCES [dbo].[tbl_Planets] ([Id]),
 CONSTRAINT [FK_Conjunctions_Planet2] FOREIGN KEY ([Planet2Id]) REFERENCES [dbo].[tbl_Planets] ([Id]),
 CONSTRAINT [FK_Conjunctions_Sign]    FOREIGN KEY ([SignId])    REFERENCES [dbo].[tbl_SignAttributes] ([Id]),
 CONSTRAINT [CK_Conjunctions_Canonical] CHECK ([Planet1Id] < [Planet2Id]),
 CONSTRAINT [CK_Conjunctions_House]     CHECK ([HouseNumberFromLagna] BETWEEN 1 AND 12),
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
-- Canonical-pair uniqueness (folded from db/06_add_chartfact_constraints.sql).
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Conjunctions_Result_Pair')
CREATE UNIQUE INDEX UX_Conjunctions_Result_Pair ON dbo.tbl_Chart_Conjunctions (ChartResultId, Planet1Id, Planet2Id);
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_Aspects]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Chart_Aspects](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ChartResultId] [int] NOT NULL,
	[AspectingPlanet] [varchar](20) NOT NULL,
	[AspectedTarget] [varchar](20) NOT NULL,
	[AspectType] [varchar](10) NOT NULL,
	[AspectingPlanetId] [tinyint] NOT NULL,
	[AspectedTargetType] [varchar](10) NOT NULL,
	[AspectedPlanetId] [tinyint] NULL,
 CONSTRAINT [FK_Aspects_Aspecting] FOREIGN KEY ([AspectingPlanetId]) REFERENCES [dbo].[tbl_Planets] ([Id]),
 CONSTRAINT [FK_Aspects_Aspected]  FOREIGN KEY ([AspectedPlanetId])  REFERENCES [dbo].[tbl_Planets] ([Id]),
 CONSTRAINT [CK_Aspects_TargetType]  CHECK ([AspectedTargetType] IN ('Planet','Ascendant')),
 CONSTRAINT [CK_Aspects_TargetShape] CHECK (
        ([AspectedTargetType] = 'Ascendant' AND [AspectedPlanetId] IS NULL) OR
        ([AspectedTargetType] = 'Planet'    AND [AspectedPlanetId] IS NOT NULL)),
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_PlanetSignTransitEvents]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_PlanetSignTransitEvents](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PlanetId] [tinyint] NOT NULL,
	[EventDateTimeUtc] [datetime2](0) NOT NULL,
	[SignId] [tinyint] NOT NULL,
	[MotionDirection] [varchar](10) NOT NULL,
	[IsReentry] [bit] NOT NULL,
	[Notes] [varchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
-- transit-event uniqueness (folded from db/04_add_chartfact_id_columns.sql).
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TransitEvent_Planet_At')
CREATE UNIQUE INDEX UX_TransitEvent_Planet_At ON dbo.tbl_PlanetSignTransitEvents (PlanetId, EventDateTimeUtc);
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_KetuSignTransitEvents]'))
EXEC dbo.sp_executesql @statement = N'
    CREATE VIEW [dbo].[vw_KetuSignTransitEvents] AS
    SELECT
        Id,
        9 AS PlanetId,  -- Ketu
        EventDateTimeUtc,
        ((SignId + 5) % 12) + 1 AS SignId,
        MotionDirection,
        IsReentry,
        Notes
    FROM tbl_PlanetSignTransitEvents
    WHERE PlanetId = 8  -- Rahu
    ' 
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tvf_PlanetSignAtDate]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
execute dbo.sp_executesql @statement = N'
CREATE FUNCTION [dbo].[tvf_PlanetSignAtDate] (@PlanetId TINYINT, @AsOfDateUtc DATETIME2(0))
RETURNS TABLE
AS
RETURN
(
    SELECT TOP (1)
        CASE WHEN @PlanetId = 9 THEN ((SignId + 5) % 12) + 1 ELSE SignId END AS SignId,
        EventDateTimeUtc,
        MotionDirection
    FROM tbl_PlanetSignTransitEvents
    WHERE PlanetId = (CASE WHEN @PlanetId = 9 THEN 8 ELSE @PlanetId END)
      AND EventDateTimeUtc <= @AsOfDateUtc
    ORDER BY EventDateTimeUtc DESC
);
' 
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_NakshatraPadaDetails]'))
EXEC dbo.sp_executesql @statement = N'
    CREATE VIEW [dbo].[vw_NakshatraPadaDetails] AS
    SELECT
        pada.Id,
        nak.Id AS NakshatraId, nak.NakshatraName,
        pada.PadaNumber, pada.StartDegree, pada.EndDegree,
        lord.Id AS NakshatraLordId, lord.PlanetName AS NakshatraLordName,
        rasi.Id AS RasiId, rasi.SignName AS RasiName,
        navamsa.Id AS NavamsaSignId, navamsa.SignName AS NavamsaSignName
    FROM tbl_NakshatraPadas pada
    JOIN tbl_Nakshatras nak ON nak.Id = pada.NakshatraId
    JOIN tbl_Planets lord ON lord.Id = nak.RulingPlanetId
    JOIN tbl_SignAttributes rasi ON rasi.Id = pada.RasiId
    JOIN tbl_SignAttributes navamsa ON navamsa.Id = pada.NavamsaSignId
    ' 
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tvf_Chart_SadeSatiPeriods]') AND type in (N'FN', N'IF', N'TF', N'FS', N'FT'))
BEGIN
execute dbo.sp_executesql @statement = N'
CREATE FUNCTION [dbo].[tvf_Chart_SadeSatiPeriods] (@BirthDetailId INT)
RETURNS TABLE
AS
RETURN
(
    WITH MoonSign AS (
        SELECT TOP (1) sa.Id AS MoonSignId
        FROM tbl_Chart_KeyDetails kd
        JOIN tbl_ChartResults cr ON cr.Id = kd.ChartResultId
        JOIN tbl_SignAttributes sa ON sa.Id = kd.SignId
        WHERE cr.BirthDetailId = @BirthDetailId AND kd.Planet = ''Moon'' AND cr.ChartTypeId = 1
    ),
    TargetSigns AS (
        SELECT ''SadeSati_Dhaiya1_Rising'' AS PeriodType, 1 AS SortOrder, ((MoonSignId - 1 + 11) % 12) + 1 AS TargetSignId FROM MoonSign
        UNION ALL SELECT ''SadeSati_Dhaiya2_Peak'',    2, ((MoonSignId - 1 + 0)  % 12) + 1 FROM MoonSign
        UNION ALL SELECT ''SadeSati_Dhaiya3_Setting'', 3, ((MoonSignId - 1 + 1)  % 12) + 1 FROM MoonSign
        UNION ALL SELECT ''KantakaShani'',             4, ((MoonSignId - 1 + 3)  % 12) + 1 FROM MoonSign
        UNION ALL SELECT ''AshtamaShani'',             5, ((MoonSignId - 1 + 7)  % 12) + 1 FROM MoonSign
    ),
    SaturnPeriods AS (
        SELECT
            SignId,
            EventDateTimeUtc AS StartDateTimeUtc,
            LEAD(EventDateTimeUtc) OVER (ORDER BY EventDateTimeUtc) AS EndDateTimeUtc
        FROM tbl_PlanetSignTransitEvents
        WHERE PlanetId = 7  -- Saturn
    )
    SELECT
        ts.PeriodType,
        ts.SortOrder,
        sp.StartDateTimeUtc,
        sp.EndDateTimeUtc,   -- NULL = still ongoing / extends past the 2060-12-31 backfill boundary
        sa.SignName AS SaturnSign
    FROM TargetSigns ts
    JOIN SaturnPeriods sp ON sp.SignId = ts.TargetSignId
    JOIN tbl_SignAttributes sa ON sa.Id = sp.SignId
);
' 
END

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_NakshatraSubLords]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_NakshatraSubLords](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[NakshatraId] [tinyint] NOT NULL,
	[SubSequenceNumber] [tinyint] NOT NULL,
	[SubLordId] [tinyint] NOT NULL,
	[StartDegree] [decimal](9, 6) NOT NULL,
	[EndDegree] [decimal](9, 6) NOT NULL,
	[RulingPlanetId]  AS ([dbo].[fn_GetNakshatraRulingPlanetId]([NakshatraId])),
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_NakshatraSubLords_Nakshatra_Seq] UNIQUE NONCLUSTERED 
(
	[NakshatraId] ASC,
	[SubSequenceNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Rule_AspectOffset]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Rule_AspectOffset](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RuleSetId] [tinyint] NOT NULL,
	[PlanetId] [tinyint] NOT NULL,
	[HouseOffset] [tinyint] NOT NULL,
	[OffsetLabel] [varchar](10) NOT NULL,
	[MethodCode] [varchar](30) NULL,
	[RuleParametersJson] [nvarchar](max) NULL,
	[CalculationNarrative] [nvarchar](max) NULL,
	[SourceRefCode] [varchar](40) NULL,
	[IsActive] [bit] NOT NULL CONSTRAINT DF_Rule_AspectOffset_IsActive DEFAULT 1,
	CONSTRAINT CK_RuleAspect_Json CHECK ([RuleParametersJson] IS NULL OR ISJSON([RuleParametersJson]) = 1),
	CONSTRAINT CK_RuleAspect_Src CHECK ([SourceRefCode] IS NULL OR [SourceRefCode] LIKE 'SRC[_]%'),
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_RuleAspectOffset] UNIQUE NONCLUSTERED 
(
	[RuleSetId] ASC,
	[PlanetId] ASC,
	[HouseOffset] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Rule_CombustionOrb]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Rule_CombustionOrb](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RuleSetId] [tinyint] NOT NULL,
	[PlanetId] [tinyint] NOT NULL,
	[DirectOrbDegrees] [decimal](5, 2) NOT NULL,
	[RetrogradeOrbDegrees] [decimal](5, 2) NULL,
	[MethodCode] [varchar](30) NULL,
	[RuleParametersJson] [nvarchar](max) NULL,
	[CalculationNarrative] [nvarchar](max) NULL,
	[SourceRefCode] [varchar](40) NULL,
	[IsActive] [bit] NOT NULL CONSTRAINT DF_Rule_CombustionOrb_IsActive DEFAULT 1,
	CONSTRAINT CK_RuleCombust_Json CHECK ([RuleParametersJson] IS NULL OR ISJSON([RuleParametersJson]) = 1),
	CONSTRAINT CK_RuleCombust_Src CHECK ([SourceRefCode] IS NULL OR [SourceRefCode] LIKE 'SRC[_]%'),
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_RuleCombustionOrb] UNIQUE NONCLUSTERED 
(
	[RuleSetId] ASC,
	[PlanetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Rule_NaturalRelationship]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Rule_NaturalRelationship](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RuleSetId] [tinyint] NOT NULL,
	[PlanetId] [tinyint] NOT NULL,
	[RelatedPlanetId] [tinyint] NOT NULL,
	[RelationshipType] [varchar](10) NOT NULL,
	[MethodCode] [varchar](30) NULL,
	[RuleParametersJson] [nvarchar](max) NULL,
	[CalculationNarrative] [nvarchar](max) NULL,
	[SourceRefCode] [varchar](40) NULL,
	[IsActive] [bit] NOT NULL CONSTRAINT DF_Rule_NaturalRelationship_IsActive DEFAULT 1,
	CONSTRAINT CK_RuleNatRel_Json CHECK ([RuleParametersJson] IS NULL OR ISJSON([RuleParametersJson]) = 1),
	CONSTRAINT CK_RuleNatRel_Src CHECK ([SourceRefCode] IS NULL OR [SourceRefCode] LIKE 'SRC[_]%'),
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_RuleNaturalRelationship] UNIQUE NONCLUSTERED 
(
	[RuleSetId] ASC,
	[PlanetId] ASC,
	[RelatedPlanetId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Rule_Sets]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Rule_Sets](
	[Id] [tinyint] NOT NULL,
	[RuleSetName] [varchar](40) NOT NULL,
	[Description] [varchar](200) NULL,
	[IsActive] [bit] NOT NULL,
	[VersionNumber] [int] NOT NULL CONSTRAINT DF_RuleSets_Version DEFAULT (1),
	[EffectiveFromUtc] [datetime2](0) NOT NULL CONSTRAINT DF_RuleSets_EffFrom DEFAULT ('2000-01-01T00:00:00'),
	[EffectiveToUtc] [datetime2](0) NULL,
	[CreatedAtUtc] [datetime2](0) NOT NULL CONSTRAINT DF_RuleSets_CreatedAt DEFAULT sysutcdatetime(),
	[SupersedesRuleSetId] [tinyint] NULL CONSTRAINT FK_RuleSets_Supersedes FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
	[SourceReference] [varchar](500) NULL,
	[IsPublished] [bit] NOT NULL CONSTRAINT DF_RuleSets_IsPublished DEFAULT (1),
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED
(
	[RuleSetName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
-- Rule-set version indexes (folded from db/03_extend_rule_sets_version.sql).
-- UX_RuleSets_OneActive is a FILTERED index; it must be built with
-- QUOTED_IDENTIFIER ON and ANSI_NULLS ON in effect.
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RuleSets_Name_Version')
CREATE UNIQUE INDEX UX_RuleSets_Name_Version ON dbo.tbl_Rule_Sets (RuleSetName, VersionNumber);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_RuleSets_OneActive')
CREATE UNIQUE INDEX UX_RuleSets_OneActive ON dbo.tbl_Rule_Sets (IsActive) WHERE IsActive = 1;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Rule_TemporaryFriendshipDistance]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[tbl_Rule_TemporaryFriendshipDistance](
	[RuleSetId] [tinyint] NOT NULL,
	[SignDistance] [tinyint] NOT NULL,
	[IsFriend] [bit] NOT NULL,
	[MethodCode] [varchar](30) NULL,
	[RuleParametersJson] [nvarchar](max) NULL,
	[CalculationNarrative] [nvarchar](max) NULL,
	[SourceRefCode] [varchar](40) NULL,
	[IsActive] [bit] NOT NULL CONSTRAINT DF_Rule_TemporaryFriendshipDistance_IsActive DEFAULT 1,
	CONSTRAINT CK_RuleTempFri_Json CHECK ([RuleParametersJson] IS NULL OR ISJSON([RuleParametersJson]) = 1),
	CONSTRAINT CK_RuleTempFri_Src CHECK ([SourceRefCode] IS NULL OR [SourceRefCode] LIKE 'SRC[_]%'),
PRIMARY KEY CLUSTERED
(
	[RuleSetId] ASC,
	[SignDistance] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_BirthDetails]') AND name = N'IX_BirthDetails_Name')
CREATE NONCLUSTERED INDEX [IX_BirthDetails_Name] ON [dbo].[tbl_BirthDetails]
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_Aspects]') AND name = N'IX_Chart_Aspects_ChartResultId')
CREATE NONCLUSTERED INDEX [IX_Chart_Aspects_ChartResultId] ON [dbo].[tbl_Chart_Aspects]
(
	[ChartResultId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_Aspects]') AND name = N'UX_Chart_Aspects_ChartResultId_AspectingPlanet_AspectedTarget')
CREATE UNIQUE NONCLUSTERED INDEX [UX_Chart_Aspects_ChartResultId_AspectingPlanet_AspectedTarget] ON [dbo].[tbl_Chart_Aspects]
(
	[ChartResultId] ASC,
	[AspectingPlanet] ASC,
	[AspectedTarget] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_Conjunctions]') AND name = N'IX_Chart_Conjunctions_ChartResultId')
CREATE NONCLUSTERED INDEX [IX_Chart_Conjunctions_ChartResultId] ON [dbo].[tbl_Chart_Conjunctions]
(
	[ChartResultId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_Conjunctions]') AND name = N'UX_Chart_Conjunctions_ChartResultId_Planet1_Planet2')
CREATE UNIQUE NONCLUSTERED INDEX [UX_Chart_Conjunctions_ChartResultId_Planet1_Planet2] ON [dbo].[tbl_Chart_Conjunctions]
(
	[ChartResultId] ASC,
	[Planet1] ASC,
	[Planet2] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_DashaPeriods]') AND name = N'IX_Chart_DashaPeriods_ChartResultId')
CREATE NONCLUSTERED INDEX [IX_Chart_DashaPeriods_ChartResultId] ON [dbo].[tbl_Chart_DashaPeriods]
(
	[ChartResultId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_DashaPeriods]') AND name = N'IX_Chart_DashaPeriods_ChartResultId_LevelNumber_StartDayOffset')
CREATE NONCLUSTERED INDEX [IX_Chart_DashaPeriods_ChartResultId_LevelNumber_StartDayOffset] ON [dbo].[tbl_Chart_DashaPeriods]
(
	[ChartResultId] ASC,
	[LevelNumber] ASC,
	[StartDayOffset] ASC,
	[EndDayOffset] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_DashaPeriods]') AND name = N'IX_Chart_DashaPeriods_ParentDashaPeriodId')
CREATE NONCLUSTERED INDEX [IX_Chart_DashaPeriods_ParentDashaPeriodId] ON [dbo].[tbl_Chart_DashaPeriods]
(
	[ParentDashaPeriodId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_HouseLords]') AND name = N'IX_Chart_HouseLords_ChartResultId')
CREATE NONCLUSTERED INDEX [IX_Chart_HouseLords_ChartResultId] ON [dbo].[tbl_Chart_HouseLords]
(
	[ChartResultId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_HouseLords]') AND name = N'UX_Chart_HouseLords_ChartResultId_HouseNumber')
CREATE UNIQUE NONCLUSTERED INDEX [UX_Chart_HouseLords_ChartResultId_HouseNumber] ON [dbo].[tbl_Chart_HouseLords]
(
	[ChartResultId] ASC,
	[HouseNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_KeyDetails]') AND name = N'IX_Chart_KeyDetails_ChartResultId')
CREATE NONCLUSTERED INDEX [IX_Chart_KeyDetails_ChartResultId] ON [dbo].[tbl_Chart_KeyDetails]
(
	[ChartResultId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Chart_KeyDetails]') AND name = N'UX_Chart_KeyDetails_ChartResultId_Planet')
CREATE UNIQUE NONCLUSTERED INDEX [UX_Chart_KeyDetails_ChartResultId_Planet] ON [dbo].[tbl_Chart_KeyDetails]
(
	[ChartResultId] ASC,
	[Planet] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_ChartResults]') AND name = N'IX_ChartResults_BirthDetailId_ChartType')
CREATE NONCLUSTERED INDEX [IX_ChartResults_BirthDetailId_ChartType] ON [dbo].[tbl_ChartResults]
(
	[BirthDetailId] ASC,
	[ChartType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Dim_LifeCalendar]') AND name = N'IX_Dim_LifeCalendar_MonthNumber')
CREATE NONCLUSTERED INDEX [IX_Dim_LifeCalendar_MonthNumber] ON [dbo].[tbl_Dim_LifeCalendar]
(
	[MonthNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Dim_LifeCalendar]') AND name = N'IX_Dim_LifeCalendar_WeekNumber')
CREATE NONCLUSTERED INDEX [IX_Dim_LifeCalendar_WeekNumber] ON [dbo].[tbl_Dim_LifeCalendar]
(
	[WeekNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_Dim_LifeCalendar]') AND name = N'IX_Dim_LifeCalendar_YearNumber')
CREATE NONCLUSTERED INDEX [IX_Dim_LifeCalendar_YearNumber] ON [dbo].[tbl_Dim_LifeCalendar]
(
	[YearNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_NakshatraPadas]') AND name = N'IX_NakshatraPadas_StartDegree')
CREATE NONCLUSTERED INDEX [IX_NakshatraPadas_StartDegree] ON [dbo].[tbl_NakshatraPadas]
(
	[StartDegree] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_NakshatraSubLords]') AND name = N'IX_NakshatraSubLords_StartDegree')
CREATE NONCLUSTERED INDEX [IX_NakshatraSubLords_StartDegree] ON [dbo].[tbl_NakshatraSubLords]
(
	[StartDegree] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[tbl_PlanetSignTransitEvents]') AND name = N'IX_PlanetSignTransitEvents_PlanetId_EventDateTimeUtc')
CREATE NONCLUSTERED INDEX [IX_PlanetSignTransitEvents_PlanetId_EventDateTimeUtc] ON [dbo].[tbl_PlanetSignTransitEvents]
(
	[PlanetId] ASC,
	[EventDateTimeUtc] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__BirthDeta__Creat__4AB81AF0]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[tbl_BirthDetails] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__ChartResu__Ayana__4E88ABD4]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[tbl_ChartResults] ADD  DEFAULT ('Lahiri') FOR [Ayanamsha]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__ChartResu__House__4F7CD00D]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[tbl_ChartResults] ADD  DEFAULT ('WholeSign') FOR [HouseSystem]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__ChartResu__Compu__5070F446]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[tbl_ChartResults] ADD  DEFAULT (sysutcdatetime()) FOR [ComputedAt]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF_Nakshatras_Straddles]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[tbl_Nakshatras] ADD  CONSTRAINT [DF_Nakshatras_Straddles]  DEFAULT ((0)) FOR [StraddlesSignBoundary]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__tbl_Plane__IsRee__43D61337]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[tbl_PlanetSignTransitEvents] ADD  DEFAULT ((0)) FOR [IsReentry]
END

GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__tbl_Rule___IsAct__6442E2C9]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[tbl_Rule_Sets] ADD  DEFAULT ((0)) FOR [IsActive]
END

GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_D1Cha__Chart__75A278F5]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_Aspects]'))
ALTER TABLE [dbo].[tbl_Chart_Aspects]  WITH CHECK ADD FOREIGN KEY([ChartResultId])
REFERENCES [dbo].[tbl_ChartResults] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_D1Cha__Chart__70DDC3D8]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_Conjunctions]'))
ALTER TABLE [dbo].[tbl_Chart_Conjunctions]  WITH CHECK ADD FOREIGN KEY([ChartResultId])
REFERENCES [dbo].[tbl_ChartResults] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Chart__Chart__17F790F9]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_DashaPeriods]'))
ALTER TABLE [dbo].[tbl_Chart_DashaPeriods]  WITH CHECK ADD FOREIGN KEY([ChartResultId])
REFERENCES [dbo].[tbl_ChartResults] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Chart__Paren__19DFD96B]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_DashaPeriods]'))
ALTER TABLE [dbo].[tbl_Chart_DashaPeriods]  WITH CHECK ADD FOREIGN KEY([ParentDashaPeriodId])
REFERENCES [dbo].[tbl_Chart_DashaPeriods] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_D1Cha__Chart__6C190EBB]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_HouseLords]'))
ALTER TABLE [dbo].[tbl_Chart_HouseLords]  WITH CHECK ADD FOREIGN KEY([ChartResultId])
REFERENCES [dbo].[tbl_ChartResults] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_D1Cha__Chart__6754599E]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_KeyDetails]'))
ALTER TABLE [dbo].[tbl_Chart_KeyDetails]  WITH CHECK ADD FOREIGN KEY([ChartResultId])
REFERENCES [dbo].[tbl_ChartResults] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ChartKeyDetails_Nakshatra]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_KeyDetails]'))
ALTER TABLE [dbo].[tbl_Chart_KeyDetails]  WITH CHECK ADD  CONSTRAINT [FK_ChartKeyDetails_Nakshatra] FOREIGN KEY([NakshatraId])
REFERENCES [dbo].[tbl_Nakshatras] ([Id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ChartKeyDetails_Nakshatra]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_KeyDetails]'))
ALTER TABLE [dbo].[tbl_Chart_KeyDetails] CHECK CONSTRAINT [FK_ChartKeyDetails_Nakshatra]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ChartKeyDetails_NakshatraPada]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_KeyDetails]'))
ALTER TABLE [dbo].[tbl_Chart_KeyDetails]  WITH CHECK ADD  CONSTRAINT [FK_ChartKeyDetails_NakshatraPada] FOREIGN KEY([NakshatraPadaId])
REFERENCES [dbo].[tbl_NakshatraPadas] ([Id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ChartKeyDetails_NakshatraPada]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_KeyDetails]'))
ALTER TABLE [dbo].[tbl_Chart_KeyDetails] CHECK CONSTRAINT [FK_ChartKeyDetails_NakshatraPada]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__ChartResu__Birth__4D94879B]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_ChartResults]'))
ALTER TABLE [dbo].[tbl_ChartResults]  WITH CHECK ADD FOREIGN KEY([BirthDetailId])
REFERENCES [dbo].[tbl_BirthDetails] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Naksh__Naksh__55009F39]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_NakshatraPadas]'))
ALTER TABLE [dbo].[tbl_NakshatraPadas]  WITH CHECK ADD FOREIGN KEY([NakshatraId])
REFERENCES [dbo].[tbl_Nakshatras] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Naksh__Navam__57DD0BE4]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_NakshatraPadas]'))
ALTER TABLE [dbo].[tbl_NakshatraPadas]  WITH CHECK ADD FOREIGN KEY([NavamsaSignId])
REFERENCES [dbo].[tbl_SignAttributes] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Naksh__RasiI__56E8E7AB]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_NakshatraPadas]'))
ALTER TABLE [dbo].[tbl_NakshatraPadas]  WITH CHECK ADD FOREIGN KEY([RasiId])
REFERENCES [dbo].[tbl_SignAttributes] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Naksh__Rulin__4B7734FF]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Nakshatras]'))
ALTER TABLE [dbo].[tbl_Nakshatras]  WITH CHECK ADD FOREIGN KEY([RulingPlanetId])
REFERENCES [dbo].[tbl_Planets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nakshatras_PrimaryRasi]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Nakshatras]'))
ALTER TABLE [dbo].[tbl_Nakshatras]  WITH CHECK ADD  CONSTRAINT [FK_Nakshatras_PrimaryRasi] FOREIGN KEY([PrimaryRasiId])
REFERENCES [dbo].[tbl_SignAttributes] ([Id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Nakshatras_PrimaryRasi]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Nakshatras]'))
ALTER TABLE [dbo].[tbl_Nakshatras] CHECK CONSTRAINT [FK_Nakshatras_PrimaryRasi]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Naksh__Naksh__5CA1C101]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_NakshatraSubLords]'))
ALTER TABLE [dbo].[tbl_NakshatraSubLords]  WITH CHECK ADD FOREIGN KEY([NakshatraId])
REFERENCES [dbo].[tbl_Nakshatras] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Naksh__SubLo__5E8A0973]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_NakshatraSubLords]'))
ALTER TABLE [dbo].[tbl_NakshatraSubLords]  WITH CHECK ADD FOREIGN KEY([SubLordId])
REFERENCES [dbo].[tbl_Planets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Plane__Plane__40F9A68C]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_PlanetSignTransitEvents]'))
ALTER TABLE [dbo].[tbl_PlanetSignTransitEvents]  WITH CHECK ADD FOREIGN KEY([PlanetId])
REFERENCES [dbo].[tbl_Planets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Plane__SignI__41EDCAC5]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_PlanetSignTransitEvents]'))
ALTER TABLE [dbo].[tbl_PlanetSignTransitEvents]  WITH CHECK ADD FOREIGN KEY([SignId])
REFERENCES [dbo].[tbl_SignAttributes] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Rule___Plane__690797E6]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_AspectOffset]'))
ALTER TABLE [dbo].[tbl_Rule_AspectOffset]  WITH CHECK ADD FOREIGN KEY([PlanetId])
REFERENCES [dbo].[tbl_Planets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Rule___RuleS__681373AD]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_AspectOffset]'))
ALTER TABLE [dbo].[tbl_Rule_AspectOffset]  WITH CHECK ADD FOREIGN KEY([RuleSetId])
REFERENCES [dbo].[tbl_Rule_Sets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Rule___Plane__6EC0713C]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_CombustionOrb]'))
ALTER TABLE [dbo].[tbl_Rule_CombustionOrb]  WITH CHECK ADD FOREIGN KEY([PlanetId])
REFERENCES [dbo].[tbl_Planets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Rule___RuleS__6DCC4D03]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_CombustionOrb]'))
ALTER TABLE [dbo].[tbl_Rule_CombustionOrb]  WITH CHECK ADD FOREIGN KEY([RuleSetId])
REFERENCES [dbo].[tbl_Rule_Sets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Rule___Plane__73852659]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_NaturalRelationship]'))
ALTER TABLE [dbo].[tbl_Rule_NaturalRelationship]  WITH CHECK ADD FOREIGN KEY([PlanetId])
REFERENCES [dbo].[tbl_Planets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Rule___Relat__74794A92]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_NaturalRelationship]'))
ALTER TABLE [dbo].[tbl_Rule_NaturalRelationship]  WITH CHECK ADD FOREIGN KEY([RelatedPlanetId])
REFERENCES [dbo].[tbl_Planets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Rule___RuleS__72910220]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_NaturalRelationship]'))
ALTER TABLE [dbo].[tbl_Rule_NaturalRelationship]  WITH CHECK ADD FOREIGN KEY([RuleSetId])
REFERENCES [dbo].[tbl_Rule_Sets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_Rule___RuleS__793DFFAF]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_TemporaryFriendshipDistance]'))
ALTER TABLE [dbo].[tbl_Rule_TemporaryFriendshipDistance]  WITH CHECK ADD FOREIGN KEY([RuleSetId])
REFERENCES [dbo].[tbl_Rule_Sets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_SignA__Debil__3D2915A8]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_SignAttributes]'))
ALTER TABLE [dbo].[tbl_SignAttributes]  WITH CHECK ADD FOREIGN KEY([DebilitatedPlanetId])
REFERENCES [dbo].[tbl_Planets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_SignA__Exalt__3C34F16F]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_SignAttributes]'))
ALTER TABLE [dbo].[tbl_SignAttributes]  WITH CHECK ADD FOREIGN KEY([ExaltedPlanetId])
REFERENCES [dbo].[tbl_Planets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_SignA__Moolt__3E1D39E1]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_SignAttributes]'))
ALTER TABLE [dbo].[tbl_SignAttributes]  WITH CHECK ADD FOREIGN KEY([MooltrikonaPlanetId])
REFERENCES [dbo].[tbl_Planets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK__tbl_SignA__Rulin__3587F3E0]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_SignAttributes]'))
ALTER TABLE [dbo].[tbl_SignAttributes]  WITH CHECK ADD FOREIGN KEY([RulingPlanetId])
REFERENCES [dbo].[tbl_Planets] ([Id])
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Chart_DashaPeriods_LevelNumber]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_DashaPeriods]'))
ALTER TABLE [dbo].[tbl_Chart_DashaPeriods]  WITH CHECK ADD  CONSTRAINT [CK_Chart_DashaPeriods_LevelNumber] CHECK  (([LevelNumber]=(3) OR [LevelNumber]=(2) OR [LevelNumber]=(1)))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Chart_DashaPeriods_LevelNumber]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_DashaPeriods]'))
ALTER TABLE [dbo].[tbl_Chart_DashaPeriods] CHECK CONSTRAINT [CK_Chart_DashaPeriods_LevelNumber]
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Chart_DashaPeriods_SequenceInParent]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_DashaPeriods]'))
ALTER TABLE [dbo].[tbl_Chart_DashaPeriods]  WITH CHECK ADD  CONSTRAINT [CK_Chart_DashaPeriods_SequenceInParent] CHECK  (([SequenceInParent]>=(1) AND [SequenceInParent]<=(9)))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_Chart_DashaPeriods_SequenceInParent]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Chart_DashaPeriods]'))
ALTER TABLE [dbo].[tbl_Chart_DashaPeriods] CHECK CONSTRAINT [CK_Chart_DashaPeriods_SequenceInParent]
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Naksh__PadaN__55F4C372]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_NakshatraPadas]'))
ALTER TABLE [dbo].[tbl_NakshatraPadas]  WITH CHECK ADD CHECK  (([PadaNumber]>=(1) AND [PadaNumber]<=(4)))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Naksh__Tatva__51300E55]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Nakshatras]'))
ALTER TABLE [dbo].[tbl_Nakshatras]  WITH CHECK ADD CHECK  (([Tatva]='Akash' OR [Tatva]='Vayu' OR [Tatva]='Agni' OR [Tatva]='Jal' OR [Tatva]='Prithvi' OR [Tatva] IS NULL))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Naksh__Varna__503BEA1C]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Nakshatras]'))
ALTER TABLE [dbo].[tbl_Nakshatras]  WITH CHECK ADD CHECK  (([Varna]='Shudra' OR [Varna]='Vaishya' OR [Varna]='Kshatriya' OR [Varna]='Brahmin' OR [Varna] IS NULL))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Naksh__YoniG__4E53A1AA]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Nakshatras]'))
ALTER TABLE [dbo].[tbl_Nakshatras]  WITH CHECK ADD CHECK  (([YoniGender]='Female' OR [YoniGender]='Male' OR [YoniGender] IS NULL))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Naksha__Gana__4D5F7D71]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Nakshatras]'))
ALTER TABLE [dbo].[tbl_Nakshatras]  WITH CHECK ADD CHECK  (([Gana]='Rakshasa' OR [Gana]='Manushya' OR [Gana]='Deva' OR [Gana] IS NULL))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Naksha__Guna__4C6B5938]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Nakshatras]'))
ALTER TABLE [dbo].[tbl_Nakshatras]  WITH CHECK ADD CHECK  (([Guna]='Tamas' OR [Guna]='Rajas' OR [Guna]='Satva' OR [Guna] IS NULL))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Naksha__Nadi__4F47C5E3]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Nakshatras]'))
ALTER TABLE [dbo].[tbl_Nakshatras]  WITH CHECK ADD CHECK  (([Nadi]='Kapha' OR [Nadi]='Pitta' OR [Nadi]='Vata' OR [Nadi] IS NULL))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Naksh__SubSe__5D95E53A]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_NakshatraSubLords]'))
ALTER TABLE [dbo].[tbl_NakshatraSubLords]  WITH CHECK ADD CHECK  (([SubSequenceNumber]>=(1) AND [SubSequenceNumber]<=(9)))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Plane__Natur__30C33EC3]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Planets]'))
ALTER TABLE [dbo].[tbl_Planets]  WITH CHECK ADD CHECK  (([NaturalNature]='Conditional' OR [NaturalNature]='Malefic' OR [NaturalNature]='Benefic'))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Plane__Motio__42E1EEFE]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_PlanetSignTransitEvents]'))
ALTER TABLE [dbo].[tbl_PlanetSignTransitEvents]  WITH CHECK ADD CHECK  (([MotionDirection]='Retrograde' OR [MotionDirection]='Direct'))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_PlanetSignTransitEvents_PlanetId]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_PlanetSignTransitEvents]'))
ALTER TABLE [dbo].[tbl_PlanetSignTransitEvents]  WITH CHECK ADD  CONSTRAINT [CK_PlanetSignTransitEvents_PlanetId] CHECK  (([PlanetId]=(8) OR [PlanetId]=(7) OR [PlanetId]=(5)))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_PlanetSignTransitEvents_PlanetId]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_PlanetSignTransitEvents]'))
ALTER TABLE [dbo].[tbl_PlanetSignTransitEvents] CHECK CONSTRAINT [CK_PlanetSignTransitEvents_PlanetId]
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Rule___House__69FBBC1F]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_AspectOffset]'))
ALTER TABLE [dbo].[tbl_Rule_AspectOffset]  WITH CHECK ADD CHECK  (([HouseOffset]>=(1) AND [HouseOffset]<=(12)))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Rule___Relat__756D6ECB]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_NaturalRelationship]'))
ALTER TABLE [dbo].[tbl_Rule_NaturalRelationship]  WITH CHECK ADD CHECK  (([RelationshipType]='Enemy' OR [RelationshipType]='Neutral' OR [RelationshipType]='Friend'))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_RuleNaturalRelationship_NotSelf]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_NaturalRelationship]'))
ALTER TABLE [dbo].[tbl_Rule_NaturalRelationship]  WITH CHECK ADD  CONSTRAINT [CK_RuleNaturalRelationship_NotSelf] CHECK  (([PlanetId]<>[RelatedPlanetId]))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_RuleNaturalRelationship_NotSelf]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_NaturalRelationship]'))
ALTER TABLE [dbo].[tbl_Rule_NaturalRelationship] CHECK CONSTRAINT [CK_RuleNaturalRelationship_NotSelf]
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_Rule___SignD__7A3223E8]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_Rule_TemporaryFriendshipDistance]'))
ALTER TABLE [dbo].[tbl_Rule_TemporaryFriendshipDistance]  WITH CHECK ADD CHECK  (([SignDistance]>=(1) AND [SignDistance]<=(12)))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_SignA__Direc__395884C4]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_SignAttributes]'))
ALTER TABLE [dbo].[tbl_SignAttributes]  WITH CHECK ADD CHECK  (([Direction]='North' OR [Direction]='West' OR [Direction]='South' OR [Direction]='East'))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_SignA__Gende__3864608B]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_SignAttributes]'))
ALTER TABLE [dbo].[tbl_SignAttributes]  WITH CHECK ADD CHECK  (([Gender]='Female' OR [Gender]='Male'))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_SignA__Risin__3A4CA8FD]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_SignAttributes]'))
ALTER TABLE [dbo].[tbl_SignAttributes]  WITH CHECK ADD CHECK  (([RisingType]='Ubhayodaya' OR [RisingType]='Prishthodaya' OR [RisingType]='Sirshodaya' OR [RisingType] IS NULL))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_SignA__Symbo__3B40CD36]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_SignAttributes]'))
ALTER TABLE [dbo].[tbl_SignAttributes]  WITH CHECK ADD CHECK  (([SymbolAnimalType]='Jalachara' OR [SymbolAnimalType]='Keeta' OR [SymbolAnimalType]='Dwipada' OR [SymbolAnimalType]='Chatushpada'))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_SignA__type___367C1819]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_SignAttributes]'))
ALTER TABLE [dbo].[tbl_SignAttributes]  WITH CHECK ADD CHECK  (([type_house_element]='Water' OR [type_house_element]='Air' OR [type_house_element]='Earth' OR [type_house_element]='Fire'))
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK__tbl_SignA__type___37703C52]') AND parent_object_id = OBJECT_ID(N'[dbo].[tbl_SignAttributes]'))
ALTER TABLE [dbo].[tbl_SignAttributes]  WITH CHECK ADD CHECK  (([type_house_keyattri]='Dwiswabhava' OR [type_house_keyattri]='Sthira' OR [type_house_keyattri]='Chara'))
GO

-- --------------------- REFERENCE DATA --------------------
-- --- tbl_Planets ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Planets)
BEGIN
INSERT [dbo].[tbl_Planets] ([Id], [PlanetName], [PlanetNameSanskrit], [NaturalNature], [ConditionalRule], [RulesSign], [VimshottariYears], [VimshottariSequenceOrder]) VALUES (1, N'Sun', N'Surya', N'Malefic', NULL, 1, 6, 3)
INSERT [dbo].[tbl_Planets] ([Id], [PlanetName], [PlanetNameSanskrit], [NaturalNature], [ConditionalRule], [RulesSign], [VimshottariYears], [VimshottariSequenceOrder]) VALUES (2, N'Moon', N'Chandra', N'Conditional', N'Benefic when waxing (Shukla Paksha)', 1, 10, 4)
INSERT [dbo].[tbl_Planets] ([Id], [PlanetName], [PlanetNameSanskrit], [NaturalNature], [ConditionalRule], [RulesSign], [VimshottariYears], [VimshottariSequenceOrder]) VALUES (3, N'Mars', N'Mangala', N'Malefic', NULL, 1, 7, 5)
INSERT [dbo].[tbl_Planets] ([Id], [PlanetName], [PlanetNameSanskrit], [NaturalNature], [ConditionalRule], [RulesSign], [VimshottariYears], [VimshottariSequenceOrder]) VALUES (4, N'Mercury', N'Budha', N'Conditional', N'Benefic when unafflicted / conjunct benefics', 1, 17, 9)
INSERT [dbo].[tbl_Planets] ([Id], [PlanetName], [PlanetNameSanskrit], [NaturalNature], [ConditionalRule], [RulesSign], [VimshottariYears], [VimshottariSequenceOrder]) VALUES (5, N'Jupiter', N'Guru', N'Benefic', NULL, 1, 16, 7)
INSERT [dbo].[tbl_Planets] ([Id], [PlanetName], [PlanetNameSanskrit], [NaturalNature], [ConditionalRule], [RulesSign], [VimshottariYears], [VimshottariSequenceOrder]) VALUES (6, N'Venus', N'Shukra', N'Benefic', NULL, 1, 20, 2)
INSERT [dbo].[tbl_Planets] ([Id], [PlanetName], [PlanetNameSanskrit], [NaturalNature], [ConditionalRule], [RulesSign], [VimshottariYears], [VimshottariSequenceOrder]) VALUES (7, N'Saturn', N'Shani', N'Malefic', NULL, 1, 19, 8)
INSERT [dbo].[tbl_Planets] ([Id], [PlanetName], [PlanetNameSanskrit], [NaturalNature], [ConditionalRule], [RulesSign], [VimshottariYears], [VimshottariSequenceOrder]) VALUES (8, N'Rahu', N'Rahu', N'Malefic', NULL, 0, 18, 6)
INSERT [dbo].[tbl_Planets] ([Id], [PlanetName], [PlanetNameSanskrit], [NaturalNature], [ConditionalRule], [RulesSign], [VimshottariYears], [VimshottariSequenceOrder]) VALUES (9, N'Ketu', N'Ketu', N'Malefic', NULL, 0, 7, 1)
END
GO

-- --- tbl_SignAttributes ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_SignAttributes)
BEGIN
INSERT [dbo].[tbl_SignAttributes] ([Id], [SignName], [SignNameSanskrit], [ZodiacEnumValue], [RulingPlanetId], [type_house_element], [type_house_keyattri], [Gender], [Direction], [RisingType], [SymbolAnimalType], [SymbolDescription], [KalapurushaBodyPart], [ExaltedPlanetId], [ExaltedDegree], [DebilitatedPlanetId], [DebilitatedDegree], [MooltrikonaPlanetId], [MooltrikonaRangeStart], [MooltrikonaRangeEnd]) VALUES (1, N'Aries', N'Mesha', N'Aries', 3, N'Fire', N'Chara', N'Male', N'East', NULL, N'Chatushpada', N'Ram', N'Head', 1, CAST(10.00 AS Decimal(5, 2)), 7, CAST(20.00 AS Decimal(5, 2)), 3, CAST(0.00 AS Decimal(5, 2)), CAST(12.00 AS Decimal(5, 2)))
INSERT [dbo].[tbl_SignAttributes] ([Id], [SignName], [SignNameSanskrit], [ZodiacEnumValue], [RulingPlanetId], [type_house_element], [type_house_keyattri], [Gender], [Direction], [RisingType], [SymbolAnimalType], [SymbolDescription], [KalapurushaBodyPart], [ExaltedPlanetId], [ExaltedDegree], [DebilitatedPlanetId], [DebilitatedDegree], [MooltrikonaPlanetId], [MooltrikonaRangeStart], [MooltrikonaRangeEnd]) VALUES (2, N'Taurus', N'Vrishabha', N'Taurus', 6, N'Earth', N'Sthira', N'Female', N'South', NULL, N'Chatushpada', N'Bull', N'Face', 2, CAST(3.00 AS Decimal(5, 2)), NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[tbl_SignAttributes] ([Id], [SignName], [SignNameSanskrit], [ZodiacEnumValue], [RulingPlanetId], [type_house_element], [type_house_keyattri], [Gender], [Direction], [RisingType], [SymbolAnimalType], [SymbolDescription], [KalapurushaBodyPart], [ExaltedPlanetId], [ExaltedDegree], [DebilitatedPlanetId], [DebilitatedDegree], [MooltrikonaPlanetId], [MooltrikonaRangeStart], [MooltrikonaRangeEnd]) VALUES (3, N'Gemini', N'Mithuna', N'Gemini', 4, N'Air', N'Dwiswabhava', N'Male', N'West', NULL, N'Dwipada', N'Twins', N'Arms/Shoulders', NULL, NULL, NULL, NULL, NULL, NULL, NULL)
INSERT [dbo].[tbl_SignAttributes] ([Id], [SignName], [SignNameSanskrit], [ZodiacEnumValue], [RulingPlanetId], [type_house_element], [type_house_keyattri], [Gender], [Direction], [RisingType], [SymbolAnimalType], [SymbolDescription], [KalapurushaBodyPart], [ExaltedPlanetId], [ExaltedDegree], [DebilitatedPlanetId], [DebilitatedDegree], [MooltrikonaPlanetId], [MooltrikonaRangeStart], [MooltrikonaRangeEnd]) VALUES (4, N'Cancer', N'Karka', N'Cancer', 2, N'Water', N'Chara', N'Female', N'North', NULL, N'Jalachara', N'Crab', N'Chest', 5, CAST(5.00 AS Decimal(5, 2)), 3, CAST(28.00 AS Decimal(5, 2)), NULL, NULL, NULL)
INSERT [dbo].[tbl_SignAttributes] ([Id], [SignName], [SignNameSanskrit], [ZodiacEnumValue], [RulingPlanetId], [type_house_element], [type_house_keyattri], [Gender], [Direction], [RisingType], [SymbolAnimalType], [SymbolDescription], [KalapurushaBodyPart], [ExaltedPlanetId], [ExaltedDegree], [DebilitatedPlanetId], [DebilitatedDegree], [MooltrikonaPlanetId], [MooltrikonaRangeStart], [MooltrikonaRangeEnd]) VALUES (5, N'Leo', N'Simha', N'Leo', 1, N'Fire', N'Sthira', N'Male', N'East', NULL, N'Chatushpada', N'Lion', N'Heart', NULL, NULL, NULL, NULL, 1, CAST(0.00 AS Decimal(5, 2)), CAST(20.00 AS Decimal(5, 2)))
INSERT [dbo].[tbl_SignAttributes] ([Id], [SignName], [SignNameSanskrit], [ZodiacEnumValue], [RulingPlanetId], [type_house_element], [type_house_keyattri], [Gender], [Direction], [RisingType], [SymbolAnimalType], [SymbolDescription], [KalapurushaBodyPart], [ExaltedPlanetId], [ExaltedDegree], [DebilitatedPlanetId], [DebilitatedDegree], [MooltrikonaPlanetId], [MooltrikonaRangeStart], [MooltrikonaRangeEnd]) VALUES (6, N'Virgo', N'Kanya', N'Virgo', 4, N'Earth', N'Dwiswabhava', N'Female', N'South', NULL, N'Dwipada', N'Virgin', N'Stomach', 4, CAST(15.00 AS Decimal(5, 2)), 6, CAST(27.00 AS Decimal(5, 2)), 4, CAST(16.00 AS Decimal(5, 2)), CAST(20.00 AS Decimal(5, 2)))
INSERT [dbo].[tbl_SignAttributes] ([Id], [SignName], [SignNameSanskrit], [ZodiacEnumValue], [RulingPlanetId], [type_house_element], [type_house_keyattri], [Gender], [Direction], [RisingType], [SymbolAnimalType], [SymbolDescription], [KalapurushaBodyPart], [ExaltedPlanetId], [ExaltedDegree], [DebilitatedPlanetId], [DebilitatedDegree], [MooltrikonaPlanetId], [MooltrikonaRangeStart], [MooltrikonaRangeEnd]) VALUES (7, N'Libra', N'Tula', N'Libra', 6, N'Air', N'Chara', N'Male', N'West', NULL, N'Dwipada', N'Scales', N'Navel/Pelvis', 7, CAST(20.00 AS Decimal(5, 2)), 1, CAST(10.00 AS Decimal(5, 2)), 6, CAST(0.00 AS Decimal(5, 2)), CAST(15.00 AS Decimal(5, 2)))
INSERT [dbo].[tbl_SignAttributes] ([Id], [SignName], [SignNameSanskrit], [ZodiacEnumValue], [RulingPlanetId], [type_house_element], [type_house_keyattri], [Gender], [Direction], [RisingType], [SymbolAnimalType], [SymbolDescription], [KalapurushaBodyPart], [ExaltedPlanetId], [ExaltedDegree], [DebilitatedPlanetId], [DebilitatedDegree], [MooltrikonaPlanetId], [MooltrikonaRangeStart], [MooltrikonaRangeEnd]) VALUES (8, N'Scorpio', N'Vrishchika', N'Scorpio', 3, N'Water', N'Sthira', N'Female', N'North', NULL, N'Keeta', N'Scorpion', N'Genitals', NULL, NULL, 2, CAST(3.00 AS Decimal(5, 2)), NULL, NULL, NULL)
INSERT [dbo].[tbl_SignAttributes] ([Id], [SignName], [SignNameSanskrit], [ZodiacEnumValue], [RulingPlanetId], [type_house_element], [type_house_keyattri], [Gender], [Direction], [RisingType], [SymbolAnimalType], [SymbolDescription], [KalapurushaBodyPart], [ExaltedPlanetId], [ExaltedDegree], [DebilitatedPlanetId], [DebilitatedDegree], [MooltrikonaPlanetId], [MooltrikonaRangeStart], [MooltrikonaRangeEnd]) VALUES (9, N'Sagittarius', N'Dhanu', N'Sagittarius', 5, N'Fire', N'Dwiswabhava', N'Male', N'East', NULL, N'Dwipada', N'Archer/Centaur', N'Thighs', NULL, NULL, NULL, NULL, 5, CAST(0.00 AS Decimal(5, 2)), CAST(10.00 AS Decimal(5, 2)))
INSERT [dbo].[tbl_SignAttributes] ([Id], [SignName], [SignNameSanskrit], [ZodiacEnumValue], [RulingPlanetId], [type_house_element], [type_house_keyattri], [Gender], [Direction], [RisingType], [SymbolAnimalType], [SymbolDescription], [KalapurushaBodyPart], [ExaltedPlanetId], [ExaltedDegree], [DebilitatedPlanetId], [DebilitatedDegree], [MooltrikonaPlanetId], [MooltrikonaRangeStart], [MooltrikonaRangeEnd]) VALUES (10, N'Capricorn', N'Makara', N'Capricornus', 7, N'Earth', N'Chara', N'Female', N'South', NULL, N'Jalachara', N'Sea-goat', N'Knees', 3, CAST(28.00 AS Decimal(5, 2)), 5, CAST(5.00 AS Decimal(5, 2)), NULL, NULL, NULL)
INSERT [dbo].[tbl_SignAttributes] ([Id], [SignName], [SignNameSanskrit], [ZodiacEnumValue], [RulingPlanetId], [type_house_element], [type_house_keyattri], [Gender], [Direction], [RisingType], [SymbolAnimalType], [SymbolDescription], [KalapurushaBodyPart], [ExaltedPlanetId], [ExaltedDegree], [DebilitatedPlanetId], [DebilitatedDegree], [MooltrikonaPlanetId], [MooltrikonaRangeStart], [MooltrikonaRangeEnd]) VALUES (11, N'Aquarius', N'Kumbha', N'Aquarius', 7, N'Air', N'Sthira', N'Male', N'West', NULL, N'Dwipada', N'Water-bearer', N'Calves/Ankles', NULL, NULL, NULL, NULL, 7, CAST(0.00 AS Decimal(5, 2)), CAST(20.00 AS Decimal(5, 2)))
INSERT [dbo].[tbl_SignAttributes] ([Id], [SignName], [SignNameSanskrit], [ZodiacEnumValue], [RulingPlanetId], [type_house_element], [type_house_keyattri], [Gender], [Direction], [RisingType], [SymbolAnimalType], [SymbolDescription], [KalapurushaBodyPart], [ExaltedPlanetId], [ExaltedDegree], [DebilitatedPlanetId], [DebilitatedDegree], [MooltrikonaPlanetId], [MooltrikonaRangeStart], [MooltrikonaRangeEnd]) VALUES (12, N'Pisces', N'Meena', N'Pisces', 5, N'Water', N'Dwiswabhava', N'Female', N'North', NULL, N'Jalachara', N'Fish', N'Feet', 6, CAST(27.00 AS Decimal(5, 2)), 4, CAST(15.00 AS Decimal(5, 2)), NULL, NULL, NULL)
END
GO

-- --- tbl_Nakshatras ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Nakshatras)
BEGIN
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (1, N'Ashwini', CAST(0.000000 AS Decimal(9, 6)), CAST(13.333333 AS Decimal(9, 6)), 9, 1, N'Ashwini Kumaras', N'Horse''s head', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (2, N'Bharani', CAST(13.333333 AS Decimal(9, 6)), CAST(26.666667 AS Decimal(9, 6)), 6, 2, N'Yama', N'Yoni (womb)', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (3, N'Krittika', CAST(26.666667 AS Decimal(9, 6)), CAST(40.000000 AS Decimal(9, 6)), 1, 3, N'Agni', N'Razor / axe', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 2, 1)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (4, N'Rohini', CAST(40.000000 AS Decimal(9, 6)), CAST(53.333333 AS Decimal(9, 6)), 2, 4, N'Brahma (Prajapati)', N'Ox-cart / chariot', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 2, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (5, N'Mrigashira', CAST(53.333333 AS Decimal(9, 6)), CAST(66.666667 AS Decimal(9, 6)), 3, 5, N'Soma (Chandra)', N'Deer''s head', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 3, 1)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (6, N'Ardra', CAST(66.666667 AS Decimal(9, 6)), CAST(80.000000 AS Decimal(9, 6)), 8, 6, N'Rudra', N'Teardrop / gem', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 3, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (7, N'Punarvasu', CAST(80.000000 AS Decimal(9, 6)), CAST(93.333333 AS Decimal(9, 6)), 5, 7, N'Aditi', N'Bow and quiver', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 3, 1)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (8, N'Pushya', CAST(93.333333 AS Decimal(9, 6)), CAST(106.666667 AS Decimal(9, 6)), 7, 8, N'Brihaspati', N'Cow''s udder / arrow', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 4, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (9, N'Ashlesha', CAST(106.666667 AS Decimal(9, 6)), CAST(120.000000 AS Decimal(9, 6)), 4, 9, N'Nagas', N'Coiled serpent', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 4, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (10, N'Magha', CAST(120.000000 AS Decimal(9, 6)), CAST(133.333333 AS Decimal(9, 6)), 9, 10, N'Pitrs (ancestors)', N'Royal throne', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 5, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (11, N'Purva Phalguni', CAST(133.333333 AS Decimal(9, 6)), CAST(146.666667 AS Decimal(9, 6)), 6, 11, N'Bhaga', N'Front legs of a bed', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 5, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (12, N'Uttara Phalguni', CAST(146.666667 AS Decimal(9, 6)), CAST(160.000000 AS Decimal(9, 6)), 1, 12, N'Aryaman', N'Back legs of a bed', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 6, 1)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (13, N'Hasta', CAST(160.000000 AS Decimal(9, 6)), CAST(173.333333 AS Decimal(9, 6)), 2, 13, N'Savitar', N'Hand / fist', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 6, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (14, N'Chitra', CAST(173.333333 AS Decimal(9, 6)), CAST(186.666667 AS Decimal(9, 6)), 3, 14, N'Tvashta (Vishwakarma)', N'Bright jewel / pearl', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 7, 1)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (15, N'Swati', CAST(186.666667 AS Decimal(9, 6)), CAST(200.000000 AS Decimal(9, 6)), 8, 15, N'Vayu', N'Young shoot swaying / coral', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 7, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (16, N'Vishakha', CAST(200.000000 AS Decimal(9, 6)), CAST(213.333333 AS Decimal(9, 6)), 5, 16, N'Indra-Agni', N'Decorated archway', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 7, 1)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (17, N'Anuradha', CAST(213.333333 AS Decimal(9, 6)), CAST(226.666667 AS Decimal(9, 6)), 7, 17, N'Mitra', N'Lotus', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 8, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (18, N'Jyeshtha', CAST(226.666667 AS Decimal(9, 6)), CAST(240.000000 AS Decimal(9, 6)), 4, 18, N'Indra', N'Circular amulet / umbrella', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 8, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (19, N'Mula', CAST(240.000000 AS Decimal(9, 6)), CAST(253.333333 AS Decimal(9, 6)), 9, 19, N'Nirriti', N'Bunch of roots / lion''s tail', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 9, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (20, N'Purva Ashadha', CAST(253.333333 AS Decimal(9, 6)), CAST(266.666667 AS Decimal(9, 6)), 6, 20, N'Apas (Water)', N'Elephant tusk / fan', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 9, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (21, N'Uttara Ashadha', CAST(266.666667 AS Decimal(9, 6)), CAST(280.000000 AS Decimal(9, 6)), 1, 21, N'Vishvedevas', N'Elephant tusk / small bed', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 10, 1)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (22, N'Shravana', CAST(280.000000 AS Decimal(9, 6)), CAST(293.333333 AS Decimal(9, 6)), 2, 22, N'Vishnu', N'Ear / three footprints', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 10, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (23, N'Dhanishta', CAST(293.333333 AS Decimal(9, 6)), CAST(306.666667 AS Decimal(9, 6)), 3, 23, N'Vasus (8 Vasus)', N'Drum / tabor', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 11, 1)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (24, N'Shatabhisha', CAST(306.666667 AS Decimal(9, 6)), CAST(320.000000 AS Decimal(9, 6)), 8, 24, N'Varuna', N'Empty circle / 100 stars', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 11, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (25, N'Purva Bhadrapada', CAST(320.000000 AS Decimal(9, 6)), CAST(333.333333 AS Decimal(9, 6)), 5, 25, N'Aja Ekapada', N'Front legs of funeral cot / sword', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 11, 1)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (26, N'Uttara Bhadrapada', CAST(333.333333 AS Decimal(9, 6)), CAST(346.666667 AS Decimal(9, 6)), 7, 26, N'Ahirbudhnya', N'Back legs of funeral cot', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 12, 0)
INSERT [dbo].[tbl_Nakshatras] ([Id], [NakshatraName], [StartDegree], [EndDegree], [RulingPlanetId], [SequenceNumber], [RulingDeity], [Symbol], [Guna], [Gana], [YoniAnimal], [YoniGender], [Nadi], [Varna], [Tatva], [Direction], [PrimaryRasiId], [StraddlesSignBoundary]) VALUES (27, N'Revati', CAST(346.666667 AS Decimal(9, 6)), CAST(360.000000 AS Decimal(9, 6)), 4, 27, N'Pushan', N'Fish / drum', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 12, 0)
END
GO

-- --- tbl_NakshatraPadas ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_NakshatraPadas)
BEGIN
SET IDENTITY_INSERT [dbo].[tbl_NakshatraPadas] ON 

INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (1, 1, 1, CAST(0.000000 AS Decimal(9, 6)), CAST(3.333333 AS Decimal(9, 6)), 1, 1)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (2, 1, 2, CAST(3.333333 AS Decimal(9, 6)), CAST(6.666666 AS Decimal(9, 6)), 1, 2)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (3, 1, 3, CAST(6.666667 AS Decimal(9, 6)), CAST(10.000000 AS Decimal(9, 6)), 1, 3)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (4, 1, 4, CAST(10.000000 AS Decimal(9, 6)), CAST(13.333333 AS Decimal(9, 6)), 1, 4)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (5, 2, 1, CAST(13.333333 AS Decimal(9, 6)), CAST(16.666666 AS Decimal(9, 6)), 1, 5)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (6, 2, 2, CAST(16.666667 AS Decimal(9, 6)), CAST(20.000000 AS Decimal(9, 6)), 1, 6)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (7, 2, 3, CAST(20.000000 AS Decimal(9, 6)), CAST(23.333333 AS Decimal(9, 6)), 1, 7)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (8, 2, 4, CAST(23.333333 AS Decimal(9, 6)), CAST(26.666666 AS Decimal(9, 6)), 1, 8)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (9, 3, 1, CAST(26.666667 AS Decimal(9, 6)), CAST(30.000000 AS Decimal(9, 6)), 1, 9)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (10, 3, 2, CAST(30.000000 AS Decimal(9, 6)), CAST(33.333333 AS Decimal(9, 6)), 2, 10)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (11, 3, 3, CAST(33.333333 AS Decimal(9, 6)), CAST(36.666666 AS Decimal(9, 6)), 2, 11)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (12, 3, 4, CAST(36.666667 AS Decimal(9, 6)), CAST(40.000000 AS Decimal(9, 6)), 2, 12)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (13, 4, 1, CAST(40.000000 AS Decimal(9, 6)), CAST(43.333333 AS Decimal(9, 6)), 2, 1)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (14, 4, 2, CAST(43.333333 AS Decimal(9, 6)), CAST(46.666666 AS Decimal(9, 6)), 2, 2)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (15, 4, 3, CAST(46.666667 AS Decimal(9, 6)), CAST(50.000000 AS Decimal(9, 6)), 2, 3)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (16, 4, 4, CAST(50.000000 AS Decimal(9, 6)), CAST(53.333333 AS Decimal(9, 6)), 2, 4)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (17, 5, 1, CAST(53.333333 AS Decimal(9, 6)), CAST(56.666666 AS Decimal(9, 6)), 2, 5)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (18, 5, 2, CAST(56.666667 AS Decimal(9, 6)), CAST(60.000000 AS Decimal(9, 6)), 2, 6)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (19, 5, 3, CAST(60.000000 AS Decimal(9, 6)), CAST(63.333333 AS Decimal(9, 6)), 3, 7)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (20, 5, 4, CAST(63.333333 AS Decimal(9, 6)), CAST(66.666666 AS Decimal(9, 6)), 3, 8)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (21, 6, 1, CAST(66.666667 AS Decimal(9, 6)), CAST(70.000000 AS Decimal(9, 6)), 3, 9)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (22, 6, 2, CAST(70.000000 AS Decimal(9, 6)), CAST(73.333333 AS Decimal(9, 6)), 3, 10)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (23, 6, 3, CAST(73.333333 AS Decimal(9, 6)), CAST(76.666666 AS Decimal(9, 6)), 3, 11)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (24, 6, 4, CAST(76.666667 AS Decimal(9, 6)), CAST(80.000000 AS Decimal(9, 6)), 3, 12)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (25, 7, 1, CAST(80.000000 AS Decimal(9, 6)), CAST(83.333333 AS Decimal(9, 6)), 3, 1)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (26, 7, 2, CAST(83.333333 AS Decimal(9, 6)), CAST(86.666666 AS Decimal(9, 6)), 3, 2)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (27, 7, 3, CAST(86.666667 AS Decimal(9, 6)), CAST(90.000000 AS Decimal(9, 6)), 3, 3)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (28, 7, 4, CAST(90.000000 AS Decimal(9, 6)), CAST(93.333333 AS Decimal(9, 6)), 4, 4)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (29, 8, 1, CAST(93.333333 AS Decimal(9, 6)), CAST(96.666666 AS Decimal(9, 6)), 4, 5)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (30, 8, 2, CAST(96.666667 AS Decimal(9, 6)), CAST(100.000000 AS Decimal(9, 6)), 4, 6)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (31, 8, 3, CAST(100.000000 AS Decimal(9, 6)), CAST(103.333333 AS Decimal(9, 6)), 4, 7)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (32, 8, 4, CAST(103.333333 AS Decimal(9, 6)), CAST(106.666666 AS Decimal(9, 6)), 4, 8)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (33, 9, 1, CAST(106.666667 AS Decimal(9, 6)), CAST(110.000000 AS Decimal(9, 6)), 4, 9)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (34, 9, 2, CAST(110.000000 AS Decimal(9, 6)), CAST(113.333333 AS Decimal(9, 6)), 4, 10)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (35, 9, 3, CAST(113.333333 AS Decimal(9, 6)), CAST(116.666666 AS Decimal(9, 6)), 4, 11)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (36, 9, 4, CAST(116.666667 AS Decimal(9, 6)), CAST(120.000000 AS Decimal(9, 6)), 4, 12)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (37, 10, 1, CAST(120.000000 AS Decimal(9, 6)), CAST(123.333333 AS Decimal(9, 6)), 5, 1)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (38, 10, 2, CAST(123.333333 AS Decimal(9, 6)), CAST(126.666666 AS Decimal(9, 6)), 5, 2)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (39, 10, 3, CAST(126.666667 AS Decimal(9, 6)), CAST(130.000000 AS Decimal(9, 6)), 5, 3)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (40, 10, 4, CAST(130.000000 AS Decimal(9, 6)), CAST(133.333333 AS Decimal(9, 6)), 5, 4)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (41, 11, 1, CAST(133.333333 AS Decimal(9, 6)), CAST(136.666666 AS Decimal(9, 6)), 5, 5)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (42, 11, 2, CAST(136.666667 AS Decimal(9, 6)), CAST(140.000000 AS Decimal(9, 6)), 5, 6)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (43, 11, 3, CAST(140.000000 AS Decimal(9, 6)), CAST(143.333333 AS Decimal(9, 6)), 5, 7)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (44, 11, 4, CAST(143.333333 AS Decimal(9, 6)), CAST(146.666666 AS Decimal(9, 6)), 5, 8)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (45, 12, 1, CAST(146.666667 AS Decimal(9, 6)), CAST(150.000000 AS Decimal(9, 6)), 5, 9)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (46, 12, 2, CAST(150.000000 AS Decimal(9, 6)), CAST(153.333333 AS Decimal(9, 6)), 6, 10)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (47, 12, 3, CAST(153.333333 AS Decimal(9, 6)), CAST(156.666666 AS Decimal(9, 6)), 6, 11)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (48, 12, 4, CAST(156.666667 AS Decimal(9, 6)), CAST(160.000000 AS Decimal(9, 6)), 6, 12)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (49, 13, 1, CAST(160.000000 AS Decimal(9, 6)), CAST(163.333333 AS Decimal(9, 6)), 6, 1)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (50, 13, 2, CAST(163.333333 AS Decimal(9, 6)), CAST(166.666666 AS Decimal(9, 6)), 6, 2)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (51, 13, 3, CAST(166.666667 AS Decimal(9, 6)), CAST(170.000000 AS Decimal(9, 6)), 6, 3)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (52, 13, 4, CAST(170.000000 AS Decimal(9, 6)), CAST(173.333333 AS Decimal(9, 6)), 6, 4)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (53, 14, 1, CAST(173.333333 AS Decimal(9, 6)), CAST(176.666666 AS Decimal(9, 6)), 6, 5)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (54, 14, 2, CAST(176.666667 AS Decimal(9, 6)), CAST(180.000000 AS Decimal(9, 6)), 6, 6)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (55, 14, 3, CAST(180.000000 AS Decimal(9, 6)), CAST(183.333333 AS Decimal(9, 6)), 7, 7)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (56, 14, 4, CAST(183.333333 AS Decimal(9, 6)), CAST(186.666666 AS Decimal(9, 6)), 7, 8)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (57, 15, 1, CAST(186.666667 AS Decimal(9, 6)), CAST(190.000000 AS Decimal(9, 6)), 7, 9)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (58, 15, 2, CAST(190.000000 AS Decimal(9, 6)), CAST(193.333333 AS Decimal(9, 6)), 7, 10)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (59, 15, 3, CAST(193.333333 AS Decimal(9, 6)), CAST(196.666666 AS Decimal(9, 6)), 7, 11)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (60, 15, 4, CAST(196.666667 AS Decimal(9, 6)), CAST(200.000000 AS Decimal(9, 6)), 7, 12)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (61, 16, 1, CAST(200.000000 AS Decimal(9, 6)), CAST(203.333333 AS Decimal(9, 6)), 7, 1)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (62, 16, 2, CAST(203.333333 AS Decimal(9, 6)), CAST(206.666666 AS Decimal(9, 6)), 7, 2)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (63, 16, 3, CAST(206.666667 AS Decimal(9, 6)), CAST(210.000000 AS Decimal(9, 6)), 7, 3)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (64, 16, 4, CAST(210.000000 AS Decimal(9, 6)), CAST(213.333333 AS Decimal(9, 6)), 8, 4)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (65, 17, 1, CAST(213.333333 AS Decimal(9, 6)), CAST(216.666666 AS Decimal(9, 6)), 8, 5)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (66, 17, 2, CAST(216.666667 AS Decimal(9, 6)), CAST(220.000000 AS Decimal(9, 6)), 8, 6)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (67, 17, 3, CAST(220.000000 AS Decimal(9, 6)), CAST(223.333333 AS Decimal(9, 6)), 8, 7)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (68, 17, 4, CAST(223.333333 AS Decimal(9, 6)), CAST(226.666666 AS Decimal(9, 6)), 8, 8)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (69, 18, 1, CAST(226.666667 AS Decimal(9, 6)), CAST(230.000000 AS Decimal(9, 6)), 8, 9)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (70, 18, 2, CAST(230.000000 AS Decimal(9, 6)), CAST(233.333333 AS Decimal(9, 6)), 8, 10)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (71, 18, 3, CAST(233.333333 AS Decimal(9, 6)), CAST(236.666666 AS Decimal(9, 6)), 8, 11)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (72, 18, 4, CAST(236.666667 AS Decimal(9, 6)), CAST(240.000000 AS Decimal(9, 6)), 8, 12)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (73, 19, 1, CAST(240.000000 AS Decimal(9, 6)), CAST(243.333333 AS Decimal(9, 6)), 9, 1)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (74, 19, 2, CAST(243.333333 AS Decimal(9, 6)), CAST(246.666666 AS Decimal(9, 6)), 9, 2)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (75, 19, 3, CAST(246.666667 AS Decimal(9, 6)), CAST(250.000000 AS Decimal(9, 6)), 9, 3)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (76, 19, 4, CAST(250.000000 AS Decimal(9, 6)), CAST(253.333333 AS Decimal(9, 6)), 9, 4)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (77, 20, 1, CAST(253.333333 AS Decimal(9, 6)), CAST(256.666666 AS Decimal(9, 6)), 9, 5)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (78, 20, 2, CAST(256.666667 AS Decimal(9, 6)), CAST(260.000000 AS Decimal(9, 6)), 9, 6)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (79, 20, 3, CAST(260.000000 AS Decimal(9, 6)), CAST(263.333333 AS Decimal(9, 6)), 9, 7)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (80, 20, 4, CAST(263.333333 AS Decimal(9, 6)), CAST(266.666666 AS Decimal(9, 6)), 9, 8)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (81, 21, 1, CAST(266.666667 AS Decimal(9, 6)), CAST(270.000000 AS Decimal(9, 6)), 9, 9)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (82, 21, 2, CAST(270.000000 AS Decimal(9, 6)), CAST(273.333333 AS Decimal(9, 6)), 10, 10)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (83, 21, 3, CAST(273.333333 AS Decimal(9, 6)), CAST(276.666666 AS Decimal(9, 6)), 10, 11)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (84, 21, 4, CAST(276.666667 AS Decimal(9, 6)), CAST(280.000000 AS Decimal(9, 6)), 10, 12)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (85, 22, 1, CAST(280.000000 AS Decimal(9, 6)), CAST(283.333333 AS Decimal(9, 6)), 10, 1)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (86, 22, 2, CAST(283.333333 AS Decimal(9, 6)), CAST(286.666666 AS Decimal(9, 6)), 10, 2)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (87, 22, 3, CAST(286.666667 AS Decimal(9, 6)), CAST(290.000000 AS Decimal(9, 6)), 10, 3)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (88, 22, 4, CAST(290.000000 AS Decimal(9, 6)), CAST(293.333333 AS Decimal(9, 6)), 10, 4)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (89, 23, 1, CAST(293.333333 AS Decimal(9, 6)), CAST(296.666666 AS Decimal(9, 6)), 10, 5)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (90, 23, 2, CAST(296.666667 AS Decimal(9, 6)), CAST(300.000000 AS Decimal(9, 6)), 10, 6)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (91, 23, 3, CAST(300.000000 AS Decimal(9, 6)), CAST(303.333333 AS Decimal(9, 6)), 11, 7)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (92, 23, 4, CAST(303.333333 AS Decimal(9, 6)), CAST(306.666666 AS Decimal(9, 6)), 11, 8)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (93, 24, 1, CAST(306.666667 AS Decimal(9, 6)), CAST(310.000000 AS Decimal(9, 6)), 11, 9)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (94, 24, 2, CAST(310.000000 AS Decimal(9, 6)), CAST(313.333333 AS Decimal(9, 6)), 11, 10)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (95, 24, 3, CAST(313.333333 AS Decimal(9, 6)), CAST(316.666666 AS Decimal(9, 6)), 11, 11)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (96, 24, 4, CAST(316.666667 AS Decimal(9, 6)), CAST(320.000000 AS Decimal(9, 6)), 11, 12)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (97, 25, 1, CAST(320.000000 AS Decimal(9, 6)), CAST(323.333333 AS Decimal(9, 6)), 11, 1)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (98, 25, 2, CAST(323.333333 AS Decimal(9, 6)), CAST(326.666666 AS Decimal(9, 6)), 11, 2)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (99, 25, 3, CAST(326.666667 AS Decimal(9, 6)), CAST(330.000000 AS Decimal(9, 6)), 11, 3)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (100, 25, 4, CAST(330.000000 AS Decimal(9, 6)), CAST(333.333333 AS Decimal(9, 6)), 12, 4)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (101, 26, 1, CAST(333.333333 AS Decimal(9, 6)), CAST(336.666666 AS Decimal(9, 6)), 12, 5)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (102, 26, 2, CAST(336.666667 AS Decimal(9, 6)), CAST(340.000000 AS Decimal(9, 6)), 12, 6)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (103, 26, 3, CAST(340.000000 AS Decimal(9, 6)), CAST(343.333333 AS Decimal(9, 6)), 12, 7)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (104, 26, 4, CAST(343.333333 AS Decimal(9, 6)), CAST(346.666666 AS Decimal(9, 6)), 12, 8)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (105, 27, 1, CAST(346.666667 AS Decimal(9, 6)), CAST(350.000000 AS Decimal(9, 6)), 12, 9)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (106, 27, 2, CAST(350.000000 AS Decimal(9, 6)), CAST(353.333333 AS Decimal(9, 6)), 12, 10)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (107, 27, 3, CAST(353.333333 AS Decimal(9, 6)), CAST(356.666666 AS Decimal(9, 6)), 12, 11)
INSERT [dbo].[tbl_NakshatraPadas] ([Id], [NakshatraId], [PadaNumber], [StartDegree], [EndDegree], [RasiId], [NavamsaSignId]) VALUES (108, 27, 4, CAST(356.666667 AS Decimal(9, 6)), CAST(360.000000 AS Decimal(9, 6)), 12, 12)
SET IDENTITY_INSERT [dbo].[tbl_NakshatraPadas] OFF
END
GO

-- --- tbl_NakshatraSubLords ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_NakshatraSubLords)
BEGIN
SET IDENTITY_INSERT [dbo].[tbl_NakshatraSubLords] ON 

INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (1, 1, 1, 9, CAST(0.000000 AS Decimal(9, 6)), CAST(0.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (2, 1, 2, 6, CAST(0.777778 AS Decimal(9, 6)), CAST(3.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (3, 1, 3, 1, CAST(3.000000 AS Decimal(9, 6)), CAST(3.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (4, 1, 4, 2, CAST(3.666667 AS Decimal(9, 6)), CAST(4.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (5, 1, 5, 3, CAST(4.777778 AS Decimal(9, 6)), CAST(5.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (6, 1, 6, 8, CAST(5.555556 AS Decimal(9, 6)), CAST(7.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (7, 1, 7, 5, CAST(7.555556 AS Decimal(9, 6)), CAST(9.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (8, 1, 8, 7, CAST(9.333333 AS Decimal(9, 6)), CAST(11.444444 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (9, 1, 9, 4, CAST(11.444444 AS Decimal(9, 6)), CAST(13.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (10, 2, 1, 6, CAST(13.333333 AS Decimal(9, 6)), CAST(15.555555 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (11, 2, 2, 1, CAST(15.555555 AS Decimal(9, 6)), CAST(16.222222 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (12, 2, 3, 2, CAST(16.222222 AS Decimal(9, 6)), CAST(17.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (13, 2, 4, 3, CAST(17.333333 AS Decimal(9, 6)), CAST(18.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (14, 2, 5, 8, CAST(18.111111 AS Decimal(9, 6)), CAST(20.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (15, 2, 6, 5, CAST(20.111111 AS Decimal(9, 6)), CAST(21.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (16, 2, 7, 7, CAST(21.888889 AS Decimal(9, 6)), CAST(24.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (17, 2, 8, 4, CAST(24.000000 AS Decimal(9, 6)), CAST(25.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (18, 2, 9, 9, CAST(25.888889 AS Decimal(9, 6)), CAST(26.666666 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (19, 3, 1, 1, CAST(26.666667 AS Decimal(9, 6)), CAST(27.333334 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (20, 3, 2, 2, CAST(27.333334 AS Decimal(9, 6)), CAST(28.444445 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (21, 3, 3, 3, CAST(28.444445 AS Decimal(9, 6)), CAST(29.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (22, 3, 4, 8, CAST(29.222223 AS Decimal(9, 6)), CAST(31.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (23, 3, 5, 5, CAST(31.222223 AS Decimal(9, 6)), CAST(33.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (24, 3, 6, 7, CAST(33.000000 AS Decimal(9, 6)), CAST(35.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (25, 3, 7, 4, CAST(35.111111 AS Decimal(9, 6)), CAST(37.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (26, 3, 8, 9, CAST(37.000000 AS Decimal(9, 6)), CAST(37.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (27, 3, 9, 6, CAST(37.777778 AS Decimal(9, 6)), CAST(40.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (28, 4, 1, 2, CAST(40.000000 AS Decimal(9, 6)), CAST(41.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (29, 4, 2, 3, CAST(41.111111 AS Decimal(9, 6)), CAST(41.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (30, 4, 3, 8, CAST(41.888889 AS Decimal(9, 6)), CAST(43.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (31, 4, 4, 5, CAST(43.888889 AS Decimal(9, 6)), CAST(45.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (32, 4, 5, 7, CAST(45.666667 AS Decimal(9, 6)), CAST(47.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (33, 4, 6, 4, CAST(47.777778 AS Decimal(9, 6)), CAST(49.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (34, 4, 7, 9, CAST(49.666667 AS Decimal(9, 6)), CAST(50.444444 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (35, 4, 8, 6, CAST(50.444444 AS Decimal(9, 6)), CAST(52.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (36, 4, 9, 1, CAST(52.666667 AS Decimal(9, 6)), CAST(53.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (37, 5, 1, 3, CAST(53.333333 AS Decimal(9, 6)), CAST(54.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (38, 5, 2, 8, CAST(54.111111 AS Decimal(9, 6)), CAST(56.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (39, 5, 3, 5, CAST(56.111111 AS Decimal(9, 6)), CAST(57.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (40, 5, 4, 7, CAST(57.888889 AS Decimal(9, 6)), CAST(60.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (41, 5, 5, 4, CAST(60.000000 AS Decimal(9, 6)), CAST(61.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (42, 5, 6, 9, CAST(61.888889 AS Decimal(9, 6)), CAST(62.666666 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (43, 5, 7, 6, CAST(62.666666 AS Decimal(9, 6)), CAST(64.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (44, 5, 8, 1, CAST(64.888889 AS Decimal(9, 6)), CAST(65.555555 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (45, 5, 9, 2, CAST(65.555555 AS Decimal(9, 6)), CAST(66.666666 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (46, 6, 1, 8, CAST(66.666667 AS Decimal(9, 6)), CAST(68.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (47, 6, 2, 5, CAST(68.666667 AS Decimal(9, 6)), CAST(70.444445 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (48, 6, 3, 7, CAST(70.444445 AS Decimal(9, 6)), CAST(72.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (49, 6, 4, 4, CAST(72.555556 AS Decimal(9, 6)), CAST(74.444445 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (50, 6, 5, 9, CAST(74.444445 AS Decimal(9, 6)), CAST(75.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (51, 6, 6, 6, CAST(75.222223 AS Decimal(9, 6)), CAST(77.444445 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (52, 6, 7, 1, CAST(77.444445 AS Decimal(9, 6)), CAST(78.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (53, 6, 8, 2, CAST(78.111111 AS Decimal(9, 6)), CAST(79.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (54, 6, 9, 3, CAST(79.222223 AS Decimal(9, 6)), CAST(80.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (55, 7, 1, 5, CAST(80.000000 AS Decimal(9, 6)), CAST(81.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (56, 7, 2, 7, CAST(81.777778 AS Decimal(9, 6)), CAST(83.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (57, 7, 3, 4, CAST(83.888889 AS Decimal(9, 6)), CAST(85.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (58, 7, 4, 9, CAST(85.777778 AS Decimal(9, 6)), CAST(86.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (59, 7, 5, 6, CAST(86.555556 AS Decimal(9, 6)), CAST(88.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (60, 7, 6, 1, CAST(88.777778 AS Decimal(9, 6)), CAST(89.444444 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (61, 7, 7, 2, CAST(89.444444 AS Decimal(9, 6)), CAST(90.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (62, 7, 8, 3, CAST(90.555556 AS Decimal(9, 6)), CAST(91.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (63, 7, 9, 8, CAST(91.333333 AS Decimal(9, 6)), CAST(93.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (64, 8, 1, 7, CAST(93.333333 AS Decimal(9, 6)), CAST(95.444444 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (65, 8, 2, 4, CAST(95.444444 AS Decimal(9, 6)), CAST(97.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (66, 8, 3, 9, CAST(97.333333 AS Decimal(9, 6)), CAST(98.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (67, 8, 4, 6, CAST(98.111111 AS Decimal(9, 6)), CAST(100.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (68, 8, 5, 1, CAST(100.333333 AS Decimal(9, 6)), CAST(101.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (69, 8, 6, 2, CAST(101.000000 AS Decimal(9, 6)), CAST(102.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (70, 8, 7, 3, CAST(102.111111 AS Decimal(9, 6)), CAST(102.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (71, 8, 8, 8, CAST(102.888889 AS Decimal(9, 6)), CAST(104.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (72, 8, 9, 5, CAST(104.888889 AS Decimal(9, 6)), CAST(106.666666 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (73, 9, 1, 4, CAST(106.666667 AS Decimal(9, 6)), CAST(108.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (74, 9, 2, 9, CAST(108.555556 AS Decimal(9, 6)), CAST(109.333334 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (75, 9, 3, 6, CAST(109.333334 AS Decimal(9, 6)), CAST(111.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (76, 9, 4, 1, CAST(111.555556 AS Decimal(9, 6)), CAST(112.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (77, 9, 5, 2, CAST(112.222223 AS Decimal(9, 6)), CAST(113.333334 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (78, 9, 6, 3, CAST(113.333334 AS Decimal(9, 6)), CAST(114.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (79, 9, 7, 8, CAST(114.111111 AS Decimal(9, 6)), CAST(116.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (80, 9, 8, 5, CAST(116.111111 AS Decimal(9, 6)), CAST(117.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (81, 9, 9, 7, CAST(117.888889 AS Decimal(9, 6)), CAST(120.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (82, 10, 1, 9, CAST(120.000000 AS Decimal(9, 6)), CAST(120.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (83, 10, 2, 6, CAST(120.777778 AS Decimal(9, 6)), CAST(123.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (84, 10, 3, 1, CAST(123.000000 AS Decimal(9, 6)), CAST(123.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (85, 10, 4, 2, CAST(123.666667 AS Decimal(9, 6)), CAST(124.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (86, 10, 5, 3, CAST(124.777778 AS Decimal(9, 6)), CAST(125.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (87, 10, 6, 8, CAST(125.555556 AS Decimal(9, 6)), CAST(127.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (88, 10, 7, 5, CAST(127.555556 AS Decimal(9, 6)), CAST(129.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (89, 10, 8, 7, CAST(129.333333 AS Decimal(9, 6)), CAST(131.444444 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (90, 10, 9, 4, CAST(131.444444 AS Decimal(9, 6)), CAST(133.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (91, 11, 1, 6, CAST(133.333333 AS Decimal(9, 6)), CAST(135.555555 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (92, 11, 2, 1, CAST(135.555555 AS Decimal(9, 6)), CAST(136.222222 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (93, 11, 3, 2, CAST(136.222222 AS Decimal(9, 6)), CAST(137.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (94, 11, 4, 3, CAST(137.333333 AS Decimal(9, 6)), CAST(138.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (95, 11, 5, 8, CAST(138.111111 AS Decimal(9, 6)), CAST(140.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (96, 11, 6, 5, CAST(140.111111 AS Decimal(9, 6)), CAST(141.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (97, 11, 7, 7, CAST(141.888889 AS Decimal(9, 6)), CAST(144.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (98, 11, 8, 4, CAST(144.000000 AS Decimal(9, 6)), CAST(145.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (99, 11, 9, 9, CAST(145.888889 AS Decimal(9, 6)), CAST(146.666666 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (100, 12, 1, 1, CAST(146.666667 AS Decimal(9, 6)), CAST(147.333334 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (101, 12, 2, 2, CAST(147.333334 AS Decimal(9, 6)), CAST(148.444445 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (102, 12, 3, 3, CAST(148.444445 AS Decimal(9, 6)), CAST(149.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (103, 12, 4, 8, CAST(149.222223 AS Decimal(9, 6)), CAST(151.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (104, 12, 5, 5, CAST(151.222223 AS Decimal(9, 6)), CAST(153.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (105, 12, 6, 7, CAST(153.000000 AS Decimal(9, 6)), CAST(155.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (106, 12, 7, 4, CAST(155.111111 AS Decimal(9, 6)), CAST(157.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (107, 12, 8, 9, CAST(157.000000 AS Decimal(9, 6)), CAST(157.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (108, 12, 9, 6, CAST(157.777778 AS Decimal(9, 6)), CAST(160.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (109, 13, 1, 2, CAST(160.000000 AS Decimal(9, 6)), CAST(161.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (110, 13, 2, 3, CAST(161.111111 AS Decimal(9, 6)), CAST(161.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (111, 13, 3, 8, CAST(161.888889 AS Decimal(9, 6)), CAST(163.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (112, 13, 4, 5, CAST(163.888889 AS Decimal(9, 6)), CAST(165.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (113, 13, 5, 7, CAST(165.666667 AS Decimal(9, 6)), CAST(167.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (114, 13, 6, 4, CAST(167.777778 AS Decimal(9, 6)), CAST(169.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (115, 13, 7, 9, CAST(169.666667 AS Decimal(9, 6)), CAST(170.444444 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (116, 13, 8, 6, CAST(170.444444 AS Decimal(9, 6)), CAST(172.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (117, 13, 9, 1, CAST(172.666667 AS Decimal(9, 6)), CAST(173.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (118, 14, 1, 3, CAST(173.333333 AS Decimal(9, 6)), CAST(174.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (119, 14, 2, 8, CAST(174.111111 AS Decimal(9, 6)), CAST(176.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (120, 14, 3, 5, CAST(176.111111 AS Decimal(9, 6)), CAST(177.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (121, 14, 4, 7, CAST(177.888889 AS Decimal(9, 6)), CAST(180.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (122, 14, 5, 4, CAST(180.000000 AS Decimal(9, 6)), CAST(181.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (123, 14, 6, 9, CAST(181.888889 AS Decimal(9, 6)), CAST(182.666666 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (124, 14, 7, 6, CAST(182.666666 AS Decimal(9, 6)), CAST(184.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (125, 14, 8, 1, CAST(184.888889 AS Decimal(9, 6)), CAST(185.555555 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (126, 14, 9, 2, CAST(185.555555 AS Decimal(9, 6)), CAST(186.666666 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (127, 15, 1, 8, CAST(186.666667 AS Decimal(9, 6)), CAST(188.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (128, 15, 2, 5, CAST(188.666667 AS Decimal(9, 6)), CAST(190.444445 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (129, 15, 3, 7, CAST(190.444445 AS Decimal(9, 6)), CAST(192.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (130, 15, 4, 4, CAST(192.555556 AS Decimal(9, 6)), CAST(194.444445 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (131, 15, 5, 9, CAST(194.444445 AS Decimal(9, 6)), CAST(195.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (132, 15, 6, 6, CAST(195.222223 AS Decimal(9, 6)), CAST(197.444445 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (133, 15, 7, 1, CAST(197.444445 AS Decimal(9, 6)), CAST(198.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (134, 15, 8, 2, CAST(198.111111 AS Decimal(9, 6)), CAST(199.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (135, 15, 9, 3, CAST(199.222223 AS Decimal(9, 6)), CAST(200.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (136, 16, 1, 5, CAST(200.000000 AS Decimal(9, 6)), CAST(201.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (137, 16, 2, 7, CAST(201.777778 AS Decimal(9, 6)), CAST(203.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (138, 16, 3, 4, CAST(203.888889 AS Decimal(9, 6)), CAST(205.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (139, 16, 4, 9, CAST(205.777778 AS Decimal(9, 6)), CAST(206.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (140, 16, 5, 6, CAST(206.555556 AS Decimal(9, 6)), CAST(208.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (141, 16, 6, 1, CAST(208.777778 AS Decimal(9, 6)), CAST(209.444444 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (142, 16, 7, 2, CAST(209.444444 AS Decimal(9, 6)), CAST(210.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (143, 16, 8, 3, CAST(210.555556 AS Decimal(9, 6)), CAST(211.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (144, 16, 9, 8, CAST(211.333333 AS Decimal(9, 6)), CAST(213.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (145, 17, 1, 7, CAST(213.333333 AS Decimal(9, 6)), CAST(215.444444 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (146, 17, 2, 4, CAST(215.444444 AS Decimal(9, 6)), CAST(217.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (147, 17, 3, 9, CAST(217.333333 AS Decimal(9, 6)), CAST(218.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (148, 17, 4, 6, CAST(218.111111 AS Decimal(9, 6)), CAST(220.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (149, 17, 5, 1, CAST(220.333333 AS Decimal(9, 6)), CAST(221.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (150, 17, 6, 2, CAST(221.000000 AS Decimal(9, 6)), CAST(222.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (151, 17, 7, 3, CAST(222.111111 AS Decimal(9, 6)), CAST(222.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (152, 17, 8, 8, CAST(222.888889 AS Decimal(9, 6)), CAST(224.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (153, 17, 9, 5, CAST(224.888889 AS Decimal(9, 6)), CAST(226.666666 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (154, 18, 1, 4, CAST(226.666667 AS Decimal(9, 6)), CAST(228.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (155, 18, 2, 9, CAST(228.555556 AS Decimal(9, 6)), CAST(229.333334 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (156, 18, 3, 6, CAST(229.333334 AS Decimal(9, 6)), CAST(231.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (157, 18, 4, 1, CAST(231.555556 AS Decimal(9, 6)), CAST(232.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (158, 18, 5, 2, CAST(232.222223 AS Decimal(9, 6)), CAST(233.333334 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (159, 18, 6, 3, CAST(233.333334 AS Decimal(9, 6)), CAST(234.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (160, 18, 7, 8, CAST(234.111111 AS Decimal(9, 6)), CAST(236.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (161, 18, 8, 5, CAST(236.111111 AS Decimal(9, 6)), CAST(237.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (162, 18, 9, 7, CAST(237.888889 AS Decimal(9, 6)), CAST(240.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (163, 19, 1, 9, CAST(240.000000 AS Decimal(9, 6)), CAST(240.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (164, 19, 2, 6, CAST(240.777778 AS Decimal(9, 6)), CAST(243.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (165, 19, 3, 1, CAST(243.000000 AS Decimal(9, 6)), CAST(243.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (166, 19, 4, 2, CAST(243.666667 AS Decimal(9, 6)), CAST(244.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (167, 19, 5, 3, CAST(244.777778 AS Decimal(9, 6)), CAST(245.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (168, 19, 6, 8, CAST(245.555556 AS Decimal(9, 6)), CAST(247.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (169, 19, 7, 5, CAST(247.555556 AS Decimal(9, 6)), CAST(249.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (170, 19, 8, 7, CAST(249.333333 AS Decimal(9, 6)), CAST(251.444444 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (171, 19, 9, 4, CAST(251.444444 AS Decimal(9, 6)), CAST(253.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (172, 20, 1, 6, CAST(253.333333 AS Decimal(9, 6)), CAST(255.555555 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (173, 20, 2, 1, CAST(255.555555 AS Decimal(9, 6)), CAST(256.222222 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (174, 20, 3, 2, CAST(256.222222 AS Decimal(9, 6)), CAST(257.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (175, 20, 4, 3, CAST(257.333333 AS Decimal(9, 6)), CAST(258.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (176, 20, 5, 8, CAST(258.111111 AS Decimal(9, 6)), CAST(260.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (177, 20, 6, 5, CAST(260.111111 AS Decimal(9, 6)), CAST(261.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (178, 20, 7, 7, CAST(261.888889 AS Decimal(9, 6)), CAST(264.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (179, 20, 8, 4, CAST(264.000000 AS Decimal(9, 6)), CAST(265.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (180, 20, 9, 9, CAST(265.888889 AS Decimal(9, 6)), CAST(266.666666 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (181, 21, 1, 1, CAST(266.666667 AS Decimal(9, 6)), CAST(267.333334 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (182, 21, 2, 2, CAST(267.333334 AS Decimal(9, 6)), CAST(268.444445 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (183, 21, 3, 3, CAST(268.444445 AS Decimal(9, 6)), CAST(269.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (184, 21, 4, 8, CAST(269.222223 AS Decimal(9, 6)), CAST(271.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (185, 21, 5, 5, CAST(271.222223 AS Decimal(9, 6)), CAST(273.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (186, 21, 6, 7, CAST(273.000000 AS Decimal(9, 6)), CAST(275.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (187, 21, 7, 4, CAST(275.111111 AS Decimal(9, 6)), CAST(277.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (188, 21, 8, 9, CAST(277.000000 AS Decimal(9, 6)), CAST(277.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (189, 21, 9, 6, CAST(277.777778 AS Decimal(9, 6)), CAST(280.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (190, 22, 1, 2, CAST(280.000000 AS Decimal(9, 6)), CAST(281.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (191, 22, 2, 3, CAST(281.111111 AS Decimal(9, 6)), CAST(281.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (192, 22, 3, 8, CAST(281.888889 AS Decimal(9, 6)), CAST(283.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (193, 22, 4, 5, CAST(283.888889 AS Decimal(9, 6)), CAST(285.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (194, 22, 5, 7, CAST(285.666667 AS Decimal(9, 6)), CAST(287.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (195, 22, 6, 4, CAST(287.777778 AS Decimal(9, 6)), CAST(289.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (196, 22, 7, 9, CAST(289.666667 AS Decimal(9, 6)), CAST(290.444444 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (197, 22, 8, 6, CAST(290.444444 AS Decimal(9, 6)), CAST(292.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (198, 22, 9, 1, CAST(292.666667 AS Decimal(9, 6)), CAST(293.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (199, 23, 1, 3, CAST(293.333333 AS Decimal(9, 6)), CAST(294.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (200, 23, 2, 8, CAST(294.111111 AS Decimal(9, 6)), CAST(296.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (201, 23, 3, 5, CAST(296.111111 AS Decimal(9, 6)), CAST(297.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (202, 23, 4, 7, CAST(297.888889 AS Decimal(9, 6)), CAST(300.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (203, 23, 5, 4, CAST(300.000000 AS Decimal(9, 6)), CAST(301.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (204, 23, 6, 9, CAST(301.888889 AS Decimal(9, 6)), CAST(302.666666 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (205, 23, 7, 6, CAST(302.666666 AS Decimal(9, 6)), CAST(304.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (206, 23, 8, 1, CAST(304.888889 AS Decimal(9, 6)), CAST(305.555555 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (207, 23, 9, 2, CAST(305.555555 AS Decimal(9, 6)), CAST(306.666666 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (208, 24, 1, 8, CAST(306.666667 AS Decimal(9, 6)), CAST(308.666667 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (209, 24, 2, 5, CAST(308.666667 AS Decimal(9, 6)), CAST(310.444445 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (210, 24, 3, 7, CAST(310.444445 AS Decimal(9, 6)), CAST(312.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (211, 24, 4, 4, CAST(312.555556 AS Decimal(9, 6)), CAST(314.444445 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (212, 24, 5, 9, CAST(314.444445 AS Decimal(9, 6)), CAST(315.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (213, 24, 6, 6, CAST(315.222223 AS Decimal(9, 6)), CAST(317.444445 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (214, 24, 7, 1, CAST(317.444445 AS Decimal(9, 6)), CAST(318.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (215, 24, 8, 2, CAST(318.111111 AS Decimal(9, 6)), CAST(319.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (216, 24, 9, 3, CAST(319.222223 AS Decimal(9, 6)), CAST(320.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (217, 25, 1, 5, CAST(320.000000 AS Decimal(9, 6)), CAST(321.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (218, 25, 2, 7, CAST(321.777778 AS Decimal(9, 6)), CAST(323.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (219, 25, 3, 4, CAST(323.888889 AS Decimal(9, 6)), CAST(325.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (220, 25, 4, 9, CAST(325.777778 AS Decimal(9, 6)), CAST(326.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (221, 25, 5, 6, CAST(326.555556 AS Decimal(9, 6)), CAST(328.777778 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (222, 25, 6, 1, CAST(328.777778 AS Decimal(9, 6)), CAST(329.444444 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (223, 25, 7, 2, CAST(329.444444 AS Decimal(9, 6)), CAST(330.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (224, 25, 8, 3, CAST(330.555556 AS Decimal(9, 6)), CAST(331.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (225, 25, 9, 8, CAST(331.333333 AS Decimal(9, 6)), CAST(333.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (226, 26, 1, 7, CAST(333.333333 AS Decimal(9, 6)), CAST(335.444444 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (227, 26, 2, 4, CAST(335.444444 AS Decimal(9, 6)), CAST(337.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (228, 26, 3, 9, CAST(337.333333 AS Decimal(9, 6)), CAST(338.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (229, 26, 4, 6, CAST(338.111111 AS Decimal(9, 6)), CAST(340.333333 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (230, 26, 5, 1, CAST(340.333333 AS Decimal(9, 6)), CAST(341.000000 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (231, 26, 6, 2, CAST(341.000000 AS Decimal(9, 6)), CAST(342.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (232, 26, 7, 3, CAST(342.111111 AS Decimal(9, 6)), CAST(342.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (233, 26, 8, 8, CAST(342.888889 AS Decimal(9, 6)), CAST(344.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (234, 26, 9, 5, CAST(344.888889 AS Decimal(9, 6)), CAST(346.666666 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (235, 27, 1, 4, CAST(346.666667 AS Decimal(9, 6)), CAST(348.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (236, 27, 2, 9, CAST(348.555556 AS Decimal(9, 6)), CAST(349.333334 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (237, 27, 3, 6, CAST(349.333334 AS Decimal(9, 6)), CAST(351.555556 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (238, 27, 4, 1, CAST(351.555556 AS Decimal(9, 6)), CAST(352.222223 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (239, 27, 5, 2, CAST(352.222223 AS Decimal(9, 6)), CAST(353.333334 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (240, 27, 6, 3, CAST(353.333334 AS Decimal(9, 6)), CAST(354.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (241, 27, 7, 8, CAST(354.111111 AS Decimal(9, 6)), CAST(356.111111 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (242, 27, 8, 5, CAST(356.111111 AS Decimal(9, 6)), CAST(357.888889 AS Decimal(9, 6)))
INSERT [dbo].[tbl_NakshatraSubLords] ([Id], [NakshatraId], [SubSequenceNumber], [SubLordId], [StartDegree], [EndDegree]) VALUES (243, 27, 9, 7, CAST(357.888889 AS Decimal(9, 6)), CAST(360.000000 AS Decimal(9, 6)))
SET IDENTITY_INSERT [dbo].[tbl_NakshatraSubLords] OFF
END
GO

-- --- tbl_PlanetSignTransitEvents ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_PlanetSignTransitEvents)
BEGIN
SET IDENTITY_INSERT [dbo].[tbl_PlanetSignTransitEvents] ON 

INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (1, 7, CAST(N'1931-04-11T21:35:09.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (2, 7, CAST(N'1931-05-25T10:19:27.0000000' AS DateTime2), 9, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (3, 7, CAST(N'1931-12-24T15:02:49.0000000' AS DateTime2), 10, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (4, 7, CAST(N'1934-03-15T17:19:13.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (5, 7, CAST(N'1934-09-13T19:30:42.0000000' AS DateTime2), 10, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (6, 7, CAST(N'1934-12-07T09:21:06.0000000' AS DateTime2), 11, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (7, 7, CAST(N'1937-02-25T22:01:10.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (8, 7, CAST(N'1939-04-27T16:56:43.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (9, 7, CAST(N'1941-06-18T12:41:29.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (10, 7, CAST(N'1941-12-14T07:08:54.0000000' AS DateTime2), 1, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (11, 7, CAST(N'1942-03-03T17:22:44.0000000' AS DateTime2), 2, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (12, 7, CAST(N'1943-08-05T13:10:19.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (13, 7, CAST(N'1943-12-16T21:21:06.0000000' AS DateTime2), 2, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (14, 7, CAST(N'1944-04-23T09:33:03.0000000' AS DateTime2), 3, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (15, 7, CAST(N'1945-09-22T12:47:49.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (16, 7, CAST(N'1945-12-22T04:50:23.0000000' AS DateTime2), 3, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (17, 7, CAST(N'1946-06-08T10:41:57.0000000' AS DateTime2), 4, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (18, 7, CAST(N'1948-07-26T08:01:38.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (19, 7, CAST(N'1950-09-20T00:31:38.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (20, 7, CAST(N'1952-11-25T14:10:47.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (21, 7, CAST(N'1953-04-24T04:44:46.0000000' AS DateTime2), 6, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (22, 7, CAST(N'1953-08-21T06:28:50.0000000' AS DateTime2), 7, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (23, 7, CAST(N'1955-11-12T06:38:40.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (24, 7, CAST(N'1958-02-08T06:26:01.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (25, 7, CAST(N'1958-06-02T04:57:25.0000000' AS DateTime2), 8, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (26, 7, CAST(N'1958-11-07T10:00:28.0000000' AS DateTime2), 9, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (27, 7, CAST(N'1961-02-01T18:33:03.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (28, 7, CAST(N'1961-09-17T17:00:14.0000000' AS DateTime2), 9, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (29, 7, CAST(N'1961-10-07T21:07:02.0000000' AS DateTime2), 10, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (30, 7, CAST(N'1964-01-27T14:08:40.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (31, 7, CAST(N'1966-04-08T23:20:38.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (32, 7, CAST(N'1966-11-03T06:26:01.0000000' AS DateTime2), 11, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (33, 7, CAST(N'1966-12-19T20:07:16.0000000' AS DateTime2), 12, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (34, 7, CAST(N'1968-06-17T01:45:28.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (35, 7, CAST(N'1968-09-28T03:40:05.0000000' AS DateTime2), 12, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (36, 7, CAST(N'1969-03-07T10:01:10.0000000' AS DateTime2), 1, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (37, 7, CAST(N'1971-04-28T04:53:12.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (38, 7, CAST(N'1973-06-10T13:49:41.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (39, 7, CAST(N'1975-07-23T11:11:29.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (40, 7, CAST(N'1977-09-07T05:45:14.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (41, 7, CAST(N'1979-11-03T19:46:53.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (42, 7, CAST(N'1980-03-14T23:37:30.0000000' AS DateTime2), 5, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (43, 7, CAST(N'1980-07-27T04:00:28.0000000' AS DateTime2), 6, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (44, 7, CAST(N'1982-10-06T01:00:28.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (45, 7, CAST(N'1984-12-21T03:18:17.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (46, 7, CAST(N'1985-05-31T21:05:38.0000000' AS DateTime2), 7, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (47, 7, CAST(N'1985-09-16T23:40:19.0000000' AS DateTime2), 8, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (48, 7, CAST(N'1987-12-16T21:22:30.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (49, 7, CAST(N'1990-03-20T20:33:59.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (50, 7, CAST(N'1990-06-20T11:44:32.0000000' AS DateTime2), 9, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (51, 7, CAST(N'1990-12-14T19:37:44.0000000' AS DateTime2), 10, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (52, 7, CAST(N'1993-03-05T13:01:10.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (53, 7, CAST(N'1993-10-15T06:39:23.0000000' AS DateTime2), 10, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (54, 7, CAST(N'1993-11-09T23:32:35.0000000' AS DateTime2), 11, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (55, 7, CAST(N'1995-06-02T04:58:50.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (56, 7, CAST(N'1995-08-09T20:34:41.0000000' AS DateTime2), 11, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (57, 7, CAST(N'1996-02-16T12:49:13.0000000' AS DateTime2), 12, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (58, 7, CAST(N'1998-04-17T07:36:20.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (59, 7, CAST(N'2000-06-06T19:28:36.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (60, 7, CAST(N'2002-07-23T02:41:01.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (61, 7, CAST(N'2003-01-08T08:22:02.0000000' AS DateTime2), 2, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (62, 7, CAST(N'2003-04-07T14:42:25.0000000' AS DateTime2), 3, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (63, 7, CAST(N'2004-09-05T23:04:27.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (64, 7, CAST(N'2005-01-13T09:35:09.0000000' AS DateTime2), 3, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (65, 7, CAST(N'2005-05-26T01:53:12.0000000' AS DateTime2), 4, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (66, 7, CAST(N'2006-11-01T01:43:22.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (67, 7, CAST(N'2007-01-10T12:33:45.0000000' AS DateTime2), 4, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (68, 7, CAST(N'2007-07-15T23:16:24.0000000' AS DateTime2), 5, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (69, 7, CAST(N'2009-09-09T18:30:56.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (70, 7, CAST(N'2011-11-15T04:42:39.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (71, 7, CAST(N'2012-05-16T01:03:59.0000000' AS DateTime2), 6, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (72, 7, CAST(N'2012-08-04T03:18:17.0000000' AS DateTime2), 7, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (73, 7, CAST(N'2014-11-02T15:24:37.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (74, 7, CAST(N'2017-01-26T14:00:56.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (75, 7, CAST(N'2017-06-20T23:08:40.0000000' AS DateTime2), 8, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (76, 7, CAST(N'2017-10-26T09:58:22.0000000' AS DateTime2), 9, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (77, 7, CAST(N'2020-01-24T04:26:29.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (78, 7, CAST(N'2022-04-29T02:22:44.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (79, 7, CAST(N'2022-07-12T09:18:59.0000000' AS DateTime2), 10, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (80, 7, CAST(N'2023-01-17T12:34:27.0000000' AS DateTime2), 11, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (81, 7, CAST(N'2025-03-29T16:14:32.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (82, 7, CAST(N'2027-06-02T23:57:53.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (83, 7, CAST(N'2027-10-20T01:43:22.0000000' AS DateTime2), 12, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (84, 7, CAST(N'2028-02-23T13:53:54.0000000' AS DateTime2), 1, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (85, 7, CAST(N'2029-08-08T07:03:17.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (86, 7, CAST(N'2029-10-05T11:22:02.0000000' AS DateTime2), 1, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (87, 7, CAST(N'2030-04-17T03:38:40.0000000' AS DateTime2), 2, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (88, 7, CAST(N'2032-05-30T21:30:56.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (89, 7, CAST(N'2034-07-12T22:53:54.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (90, 7, CAST(N'2036-08-27T15:06:20.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (91, 7, CAST(N'2038-10-22T11:26:15.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (92, 7, CAST(N'2039-04-05T15:34:27.0000000' AS DateTime2), 5, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (93, 7, CAST(N'2039-07-12T20:29:46.0000000' AS DateTime2), 6, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (94, 7, CAST(N'2041-01-27T21:35:52.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (95, 7, CAST(N'2041-02-06T09:29:32.0000000' AS DateTime2), 6, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (96, 7, CAST(N'2041-09-26T00:52:02.0000000' AS DateTime2), 7, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (97, 7, CAST(N'2043-12-11T17:54:23.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (98, 7, CAST(N'2044-06-23T02:14:18.0000000' AS DateTime2), 7, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (99, 7, CAST(N'2044-08-30T01:29:18.0000000' AS DateTime2), 8, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (100, 7, CAST(N'2046-12-07T18:45:00.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (101, 7, CAST(N'2049-03-06T11:04:27.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (102, 7, CAST(N'2049-07-09T19:53:54.0000000' AS DateTime2), 9, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (103, 7, CAST(N'2049-12-04T02:17:07.0000000' AS DateTime2), 10, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (104, 7, CAST(N'2052-02-24T22:32:07.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (105, 7, CAST(N'2054-05-14T14:39:37.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (106, 7, CAST(N'2054-09-01T23:30:28.0000000' AS DateTime2), 11, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (107, 7, CAST(N'2055-02-05T12:33:45.0000000' AS DateTime2), 12, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (108, 7, CAST(N'2057-04-07T02:26:57.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (109, 7, CAST(N'2059-05-27T18:18:59.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (110, 5, CAST(N'1930-05-26T17:59:18.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (111, 5, CAST(N'1931-06-14T10:58:08.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (112, 5, CAST(N'1932-07-07T23:15:42.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (113, 5, CAST(N'1932-12-24T09:00:42.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (114, 5, CAST(N'1933-01-22T17:51:34.0000000' AS DateTime2), 5, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (115, 5, CAST(N'1933-08-06T08:52:58.0000000' AS DateTime2), 6, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (116, 5, CAST(N'1934-01-25T11:42:25.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (117, 5, CAST(N'1934-02-19T22:38:26.0000000' AS DateTime2), 6, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (118, 5, CAST(N'1934-09-06T17:09:23.0000000' AS DateTime2), 7, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (119, 5, CAST(N'1935-02-23T07:59:32.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (120, 5, CAST(N'1935-03-24T22:08:54.0000000' AS DateTime2), 7, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (121, 5, CAST(N'1935-10-06T06:07:02.0000000' AS DateTime2), 8, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (122, 5, CAST(N'1936-03-10T16:58:50.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (123, 5, CAST(N'1936-05-11T20:34:41.0000000' AS DateTime2), 8, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (124, 5, CAST(N'1936-10-29T13:46:53.0000000' AS DateTime2), 9, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (125, 5, CAST(N'1937-03-22T01:39:51.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (126, 5, CAST(N'1937-07-10T20:32:35.0000000' AS DateTime2), 9, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (127, 5, CAST(N'1937-11-14T04:48:59.0000000' AS DateTime2), 10, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (128, 5, CAST(N'1938-03-31T10:40:33.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (129, 5, CAST(N'1938-09-29T23:23:26.0000000' AS DateTime2), 10, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (130, 5, CAST(N'1938-11-07T09:18:59.0000000' AS DateTime2), 11, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (131, 5, CAST(N'1939-04-09T00:58:22.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (132, 5, CAST(N'1940-04-16T19:36:20.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (133, 5, CAST(N'1941-04-26T21:23:54.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (134, 5, CAST(N'1942-05-09T17:13:36.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (135, 5, CAST(N'1942-10-06T06:45:42.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (136, 5, CAST(N'1942-12-19T16:29:18.0000000' AS DateTime2), 3, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (137, 5, CAST(N'1943-05-27T03:38:40.0000000' AS DateTime2), 4, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (138, 5, CAST(N'1943-10-23T02:50:52.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (139, 5, CAST(N'1944-02-04T09:02:49.0000000' AS DateTime2), 4, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (140, 5, CAST(N'1944-06-18T19:51:48.0000000' AS DateTime2), 5, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (141, 5, CAST(N'1944-11-18T02:47:21.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (142, 5, CAST(N'1945-03-09T13:41:15.0000000' AS DateTime2), 5, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (143, 5, CAST(N'1945-07-18T06:10:33.0000000' AS DateTime2), 6, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (144, 5, CAST(N'1945-12-19T01:06:06.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (145, 5, CAST(N'1946-04-08T04:50:23.0000000' AS DateTime2), 6, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (146, 5, CAST(N'1946-08-18T21:37:58.0000000' AS DateTime2), 7, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (147, 5, CAST(N'1947-01-17T20:20:38.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (148, 5, CAST(N'1947-05-11T01:15:14.0000000' AS DateTime2), 7, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (149, 5, CAST(N'1947-09-17T00:09:51.0000000' AS DateTime2), 8, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (150, 5, CAST(N'1948-02-11T05:19:55.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (151, 5, CAST(N'1948-06-22T15:14:46.0000000' AS DateTime2), 8, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (152, 5, CAST(N'1948-10-07T20:13:36.0000000' AS DateTime2), 9, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (153, 5, CAST(N'1949-02-28T02:31:53.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (154, 5, CAST(N'1949-08-27T02:08:40.0000000' AS DateTime2), 9, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (155, 5, CAST(N'1949-10-11T08:26:15.0000000' AS DateTime2), 10, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (156, 5, CAST(N'1950-03-13T04:41:15.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (157, 5, CAST(N'1951-03-23T11:43:08.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (158, 5, CAST(N'1952-03-31T12:40:05.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (159, 5, CAST(N'1953-04-09T14:07:58.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (160, 5, CAST(N'1953-08-30T04:15:56.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (161, 5, CAST(N'1953-11-30T01:18:03.0000000' AS DateTime2), 2, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (162, 5, CAST(N'1954-04-19T22:48:59.0000000' AS DateTime2), 3, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (163, 5, CAST(N'1954-09-09T13:18:45.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (164, 5, CAST(N'1955-01-28T12:27:25.0000000' AS DateTime2), 3, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (165, 5, CAST(N'1955-05-03T10:40:33.0000000' AS DateTime2), 4, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (166, 5, CAST(N'1955-10-01T11:50:52.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (167, 5, CAST(N'1956-03-14T10:23:40.0000000' AS DateTime2), 4, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (168, 5, CAST(N'1956-05-22T04:03:59.0000000' AS DateTime2), 5, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (169, 5, CAST(N'1956-10-28T16:53:12.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (170, 5, CAST(N'1957-04-18T01:11:01.0000000' AS DateTime2), 5, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (171, 5, CAST(N'1957-06-19T11:12:53.0000000' AS DateTime2), 6, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (172, 5, CAST(N'1957-11-28T13:34:13.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (173, 5, CAST(N'1958-05-17T18:22:30.0000000' AS DateTime2), 6, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (174, 5, CAST(N'1958-07-21T14:00:56.0000000' AS DateTime2), 7, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (175, 5, CAST(N'1958-12-28T07:28:36.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (176, 5, CAST(N'1959-06-22T06:48:31.0000000' AS DateTime2), 7, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (177, 5, CAST(N'1959-08-17T11:10:47.0000000' AS DateTime2), 8, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (178, 5, CAST(N'1960-01-22T15:58:22.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (179, 5, CAST(N'1961-02-10T06:05:38.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (180, 5, CAST(N'1962-02-24T17:49:27.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (181, 5, CAST(N'1963-03-07T13:09:37.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (182, 5, CAST(N'1964-03-14T21:01:24.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (183, 5, CAST(N'1964-08-03T17:00:14.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (184, 5, CAST(N'1964-10-26T18:26:43.0000000' AS DateTime2), 1, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (185, 5, CAST(N'1965-03-21T05:24:51.0000000' AS DateTime2), 2, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (186, 5, CAST(N'1965-08-05T20:22:02.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (187, 5, CAST(N'1966-01-09T21:47:07.0000000' AS DateTime2), 2, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (188, 5, CAST(N'1966-03-23T23:24:08.0000000' AS DateTime2), 3, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (189, 5, CAST(N'1966-08-21T17:49:27.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (190, 5, CAST(N'1967-09-14T08:38:12.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (191, 5, CAST(N'1968-10-12T00:49:13.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (192, 5, CAST(N'1969-11-11T19:23:40.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (193, 5, CAST(N'1970-12-11T10:22:16.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (194, 5, CAST(N'1972-01-06T01:56:43.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (195, 5, CAST(N'1973-01-25T07:21:34.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (196, 5, CAST(N'1974-02-09T05:00:14.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (197, 5, CAST(N'1975-02-19T12:59:46.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (198, 5, CAST(N'1975-07-18T16:11:43.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (199, 5, CAST(N'1975-09-10T19:08:54.0000000' AS DateTime2), 12, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (200, 5, CAST(N'1976-02-25T12:37:16.0000000' AS DateTime2), 1, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (201, 5, CAST(N'1976-07-08T12:38:40.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (202, 5, CAST(N'1976-12-08T09:04:55.0000000' AS DateTime2), 1, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (203, 5, CAST(N'1977-02-22T13:21:34.0000000' AS DateTime2), 2, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (204, 5, CAST(N'1977-07-18T05:24:08.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (205, 5, CAST(N'1978-08-05T04:41:57.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (206, 5, CAST(N'1979-08-29T13:43:22.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (207, 5, CAST(N'1980-09-26T12:06:20.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (208, 5, CAST(N'1981-10-27T07:58:50.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (209, 5, CAST(N'1982-11-26T00:26:43.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (210, 5, CAST(N'1983-12-21T21:30:56.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (211, 5, CAST(N'1985-01-10T09:06:20.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (212, 5, CAST(N'1986-01-25T01:30:42.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (213, 5, CAST(N'1987-02-02T19:37:44.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (214, 5, CAST(N'1987-06-16T16:13:08.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (215, 5, CAST(N'1987-10-25T23:03:45.0000000' AS DateTime2), 12, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (216, 5, CAST(N'1988-02-02T21:04:55.0000000' AS DateTime2), 1, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (217, 5, CAST(N'1988-06-19T17:34:41.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (218, 5, CAST(N'1989-07-02T00:09:08.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (219, 5, CAST(N'1990-07-20T18:13:22.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (220, 5, CAST(N'1991-08-14T10:08:54.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (221, 5, CAST(N'1992-09-11T13:18:03.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (222, 5, CAST(N'1993-10-12T12:59:04.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (223, 5, CAST(N'1994-11-11T06:47:07.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (224, 5, CAST(N'1995-12-07T01:26:29.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (225, 5, CAST(N'1996-12-26T02:28:22.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (226, 5, CAST(N'1998-01-08T10:21:34.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (227, 5, CAST(N'1998-05-25T22:47:35.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (228, 5, CAST(N'1998-09-10T05:41:01.0000000' AS DateTime2), 11, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (229, 5, CAST(N'1999-01-12T21:51:20.0000000' AS DateTime2), 12, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (230, 5, CAST(N'1999-05-26T11:00:56.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (231, 5, CAST(N'2000-06-02T13:32:49.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (232, 5, CAST(N'2001-06-16T01:56:43.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (233, 5, CAST(N'2002-07-05T06:52:02.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (234, 5, CAST(N'2003-07-30T06:26:01.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (235, 5, CAST(N'2004-08-27T18:09:08.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (236, 5, CAST(N'2005-09-28T00:05:38.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (237, 5, CAST(N'2006-10-27T16:48:59.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (238, 5, CAST(N'2007-11-21T23:33:59.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (239, 5, CAST(N'2008-12-09T18:11:57.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (240, 5, CAST(N'2009-05-01T13:08:12.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (241, 5, CAST(N'2009-07-30T14:17:07.0000000' AS DateTime2), 10, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (242, 5, CAST(N'2009-12-19T18:46:24.0000000' AS DateTime2), 11, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (243, 5, CAST(N'2010-05-02T02:38:12.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (244, 5, CAST(N'2010-11-01T07:24:23.0000000' AS DateTime2), 11, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (245, 5, CAST(N'2010-12-06T03:40:05.0000000' AS DateTime2), 12, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (246, 5, CAST(N'2011-05-08T08:43:50.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (247, 5, CAST(N'2012-05-17T04:04:41.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (248, 5, CAST(N'2013-05-31T01:19:27.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (249, 5, CAST(N'2014-06-19T03:17:35.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (250, 5, CAST(N'2015-07-14T00:55:33.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (251, 5, CAST(N'2016-08-11T15:58:22.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (252, 5, CAST(N'2017-09-12T01:21:34.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (253, 5, CAST(N'2018-10-11T13:49:41.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (254, 5, CAST(N'2019-03-29T14:37:30.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (255, 5, CAST(N'2019-04-22T19:41:15.0000000' AS DateTime2), 8, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (256, 5, CAST(N'2019-11-04T23:48:03.0000000' AS DateTime2), 9, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (257, 5, CAST(N'2020-03-29T22:24:23.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (258, 5, CAST(N'2020-06-29T23:52:16.0000000' AS DateTime2), 9, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (259, 5, CAST(N'2020-11-20T07:53:12.0000000' AS DateTime2), 10, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (260, 5, CAST(N'2021-04-05T18:54:51.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (261, 5, CAST(N'2021-09-14T08:51:34.0000000' AS DateTime2), 10, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (262, 5, CAST(N'2021-11-20T18:01:24.0000000' AS DateTime2), 11, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (263, 5, CAST(N'2022-04-13T10:20:09.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (264, 5, CAST(N'2023-04-21T23:44:32.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (265, 5, CAST(N'2024-05-01T07:30:00.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (266, 5, CAST(N'2025-05-14T17:06:34.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (267, 5, CAST(N'2025-10-18T14:17:07.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (268, 5, CAST(N'2025-12-05T11:57:11.0000000' AS DateTime2), 3, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (269, 5, CAST(N'2026-06-01T20:19:55.0000000' AS DateTime2), 4, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (270, 5, CAST(N'2026-10-31T06:32:21.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (271, 5, CAST(N'2027-01-24T20:02:21.0000000' AS DateTime2), 4, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (272, 5, CAST(N'2027-06-25T23:48:45.0000000' AS DateTime2), 5, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (273, 5, CAST(N'2027-11-26T13:15:14.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (274, 5, CAST(N'2028-02-28T13:48:17.0000000' AS DateTime2), 5, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (275, 5, CAST(N'2028-07-24T10:06:48.0000000' AS DateTime2), 6, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (276, 5, CAST(N'2028-12-26T08:09:23.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (277, 5, CAST(N'2029-03-29T09:02:49.0000000' AS DateTime2), 6, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (278, 5, CAST(N'2029-08-24T19:31:24.0000000' AS DateTime2), 7, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (279, 5, CAST(N'2030-01-24T20:24:08.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (280, 5, CAST(N'2030-05-01T08:48:03.0000000' AS DateTime2), 7, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (281, 5, CAST(N'2030-09-22T20:55:47.0000000' AS DateTime2), 8, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (282, 5, CAST(N'2031-02-17T09:37:16.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (283, 5, CAST(N'2031-06-13T21:57:39.0000000' AS DateTime2), 8, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (284, 5, CAST(N'2031-10-15T10:39:08.0000000' AS DateTime2), 9, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (285, 5, CAST(N'2032-03-05T08:43:08.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (286, 5, CAST(N'2032-08-12T14:05:09.0000000' AS DateTime2), 9, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (287, 5, CAST(N'2032-10-23T15:50:38.0000000' AS DateTime2), 10, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (288, 5, CAST(N'2033-03-17T21:09:08.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (289, 5, CAST(N'2034-03-28T00:21:48.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (290, 5, CAST(N'2035-04-06T06:29:32.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (291, 5, CAST(N'2036-04-14T22:27:11.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (292, 5, CAST(N'2036-09-09T18:23:54.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (293, 5, CAST(N'2036-11-17T00:23:54.0000000' AS DateTime2), 2, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (294, 5, CAST(N'2037-04-26T09:43:36.0000000' AS DateTime2), 3, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (295, 5, CAST(N'2037-09-16T09:04:13.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (296, 5, CAST(N'2038-01-17T12:36:34.0000000' AS DateTime2), 3, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (297, 5, CAST(N'2038-05-11T14:22:02.0000000' AS DateTime2), 4, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (298, 5, CAST(N'2038-10-07T07:50:23.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (299, 5, CAST(N'2039-03-03T13:12:25.0000000' AS DateTime2), 4, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (300, 5, CAST(N'2039-06-02T00:36:34.0000000' AS DateTime2), 5, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (301, 5, CAST(N'2039-11-04T01:20:09.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (302, 5, CAST(N'2040-04-06T04:19:27.0000000' AS DateTime2), 5, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (303, 5, CAST(N'2040-06-29T12:17:35.0000000' AS DateTime2), 6, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (304, 5, CAST(N'2040-12-03T15:44:18.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (305, 5, CAST(N'2041-05-06T12:35:09.0000000' AS DateTime2), 6, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (306, 5, CAST(N'2041-07-31T00:12:39.0000000' AS DateTime2), 7, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (307, 5, CAST(N'2042-01-02T07:01:53.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (308, 5, CAST(N'2042-06-09T23:15:00.0000000' AS DateTime2), 7, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (309, 5, CAST(N'2042-08-27T22:22:58.0000000' AS DateTime2), 8, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (310, 5, CAST(N'2043-01-27T15:01:24.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (311, 5, CAST(N'2043-07-30T05:08:40.0000000' AS DateTime2), 8, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (312, 5, CAST(N'2043-09-11T08:12:53.0000000' AS DateTime2), 9, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (313, 5, CAST(N'2044-02-16T05:29:04.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (314, 5, CAST(N'2045-03-01T19:20:09.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (315, 5, CAST(N'2046-03-12T20:31:53.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (316, 5, CAST(N'2047-03-21T16:51:06.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (317, 5, CAST(N'2047-08-18T16:20:09.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (318, 5, CAST(N'2047-10-11T01:50:23.0000000' AS DateTime2), 1, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (319, 5, CAST(N'2048-03-28T04:41:15.0000000' AS DateTime2), 2, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (320, 5, CAST(N'2048-08-12T21:00:00.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (321, 5, CAST(N'2048-12-27T17:00:14.0000000' AS DateTime2), 2, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (322, 5, CAST(N'2049-04-03T08:11:29.0000000' AS DateTime2), 3, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (323, 5, CAST(N'2049-08-27T07:28:36.0000000' AS DateTime2), 4, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (324, 5, CAST(N'2050-03-07T20:10:47.0000000' AS DateTime2), 3, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (325, 5, CAST(N'2050-04-01T23:29:04.0000000' AS DateTime2), 4, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (326, 5, CAST(N'2050-09-19T05:10:05.0000000' AS DateTime2), 5, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (327, 5, CAST(N'2051-10-17T12:35:52.0000000' AS DateTime2), 6, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (328, 5, CAST(N'2052-11-16T05:06:34.0000000' AS DateTime2), 7, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (329, 5, CAST(N'2053-12-15T22:46:53.0000000' AS DateTime2), 8, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (330, 5, CAST(N'2055-01-10T19:03:59.0000000' AS DateTime2), 9, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (331, 5, CAST(N'2056-01-31T05:01:38.0000000' AS DateTime2), 10, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (332, 5, CAST(N'2057-02-14T07:54:37.0000000' AS DateTime2), 11, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (333, 5, CAST(N'2058-02-25T01:30:00.0000000' AS DateTime2), 12, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (334, 5, CAST(N'2059-03-03T23:52:58.0000000' AS DateTime2), 1, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (335, 5, CAST(N'2059-07-16T19:35:38.0000000' AS DateTime2), 2, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (336, 5, CAST(N'2059-11-26T02:04:27.0000000' AS DateTime2), 1, N'Retrograde', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (337, 5, CAST(N'2060-03-04T15:21:48.0000000' AS DateTime2), 2, N'Direct', 1, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (338, 5, CAST(N'2060-07-23T18:11:57.0000000' AS DateTime2), 3, N'Direct', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (339, 8, CAST(N'1930-10-31T02:15:42.0000000' AS DateTime2), 12, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (340, 8, CAST(N'1932-05-19T05:12:11.0000000' AS DateTime2), 11, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (341, 8, CAST(N'1933-12-06T08:08:40.0000000' AS DateTime2), 10, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (342, 8, CAST(N'1935-06-25T11:04:27.0000000' AS DateTime2), 9, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (343, 8, CAST(N'1937-01-11T14:00:56.0000000' AS DateTime2), 8, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (344, 8, CAST(N'1938-07-31T16:57:25.0000000' AS DateTime2), 7, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (345, 8, CAST(N'1940-02-17T19:53:54.0000000' AS DateTime2), 6, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (346, 8, CAST(N'1941-09-05T22:49:41.0000000' AS DateTime2), 5, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (347, 8, CAST(N'1943-03-26T01:46:10.0000000' AS DateTime2), 4, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (348, 8, CAST(N'1944-10-12T04:42:39.0000000' AS DateTime2), 3, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (349, 8, CAST(N'1946-05-01T07:39:08.0000000' AS DateTime2), 2, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (350, 8, CAST(N'1947-11-18T10:35:38.0000000' AS DateTime2), 1, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (351, 8, CAST(N'1949-06-06T13:32:07.0000000' AS DateTime2), 12, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (352, 8, CAST(N'1950-12-24T16:28:36.0000000' AS DateTime2), 11, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (353, 8, CAST(N'1952-07-12T19:25:05.0000000' AS DateTime2), 10, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (354, 8, CAST(N'1954-01-29T22:21:34.0000000' AS DateTime2), 9, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (355, 8, CAST(N'1955-08-19T01:18:03.0000000' AS DateTime2), 8, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (356, 8, CAST(N'1957-03-07T04:14:32.0000000' AS DateTime2), 7, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (357, 8, CAST(N'1958-09-24T07:11:01.0000000' AS DateTime2), 6, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (358, 8, CAST(N'1960-04-12T10:07:30.0000000' AS DateTime2), 5, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (359, 8, CAST(N'1961-10-30T13:04:41.0000000' AS DateTime2), 4, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (360, 8, CAST(N'1963-05-19T16:01:10.0000000' AS DateTime2), 3, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (361, 8, CAST(N'1964-12-05T18:57:39.0000000' AS DateTime2), 2, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (362, 8, CAST(N'1966-06-24T21:54:08.0000000' AS DateTime2), 1, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (363, 8, CAST(N'1968-01-12T00:51:20.0000000' AS DateTime2), 12, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (364, 8, CAST(N'1969-07-31T03:47:49.0000000' AS DateTime2), 11, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (365, 8, CAST(N'1971-02-17T06:44:18.0000000' AS DateTime2), 10, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (366, 8, CAST(N'1972-09-05T09:41:29.0000000' AS DateTime2), 9, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (367, 8, CAST(N'1974-03-25T12:37:58.0000000' AS DateTime2), 8, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (368, 8, CAST(N'1975-10-12T15:35:09.0000000' AS DateTime2), 7, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (369, 8, CAST(N'1977-04-30T18:31:38.0000000' AS DateTime2), 6, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (370, 8, CAST(N'1978-11-17T21:28:50.0000000' AS DateTime2), 5, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (371, 8, CAST(N'1980-06-06T00:25:19.0000000' AS DateTime2), 4, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (372, 8, CAST(N'1981-12-24T03:22:30.0000000' AS DateTime2), 3, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (373, 8, CAST(N'1983-07-13T06:19:41.0000000' AS DateTime2), 2, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (374, 8, CAST(N'1985-01-29T09:16:10.0000000' AS DateTime2), 1, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (375, 8, CAST(N'1986-08-18T12:13:22.0000000' AS DateTime2), 12, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (376, 8, CAST(N'1988-03-06T15:10:33.0000000' AS DateTime2), 11, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (377, 8, CAST(N'1989-09-23T18:07:44.0000000' AS DateTime2), 10, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (378, 8, CAST(N'1991-04-12T21:04:13.0000000' AS DateTime2), 9, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (379, 8, CAST(N'1992-10-30T00:01:24.0000000' AS DateTime2), 8, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (380, 8, CAST(N'1994-05-19T02:58:36.0000000' AS DateTime2), 7, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (381, 8, CAST(N'1995-12-06T05:55:47.0000000' AS DateTime2), 6, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (382, 8, CAST(N'1997-06-24T08:52:58.0000000' AS DateTime2), 5, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (383, 8, CAST(N'1999-01-11T11:50:09.0000000' AS DateTime2), 4, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (384, 8, CAST(N'2000-07-30T14:47:21.0000000' AS DateTime2), 3, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (385, 8, CAST(N'2002-02-16T17:44:32.0000000' AS DateTime2), 2, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (386, 8, CAST(N'2003-09-05T20:41:43.0000000' AS DateTime2), 1, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (387, 8, CAST(N'2005-03-24T23:38:54.0000000' AS DateTime2), 12, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (388, 8, CAST(N'2006-10-12T02:36:48.0000000' AS DateTime2), 11, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (389, 8, CAST(N'2008-04-30T05:33:59.0000000' AS DateTime2), 10, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (390, 8, CAST(N'2009-11-17T08:31:10.0000000' AS DateTime2), 9, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (391, 8, CAST(N'2011-06-06T11:28:22.0000000' AS DateTime2), 8, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (392, 8, CAST(N'2012-12-23T14:26:15.0000000' AS DateTime2), 7, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (393, 8, CAST(N'2014-07-12T17:23:26.0000000' AS DateTime2), 6, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (394, 8, CAST(N'2016-01-29T20:20:38.0000000' AS DateTime2), 5, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (395, 8, CAST(N'2017-08-17T23:18:31.0000000' AS DateTime2), 4, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (396, 8, CAST(N'2019-03-07T02:15:42.0000000' AS DateTime2), 3, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (397, 8, CAST(N'2020-09-23T05:13:36.0000000' AS DateTime2), 2, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (398, 8, CAST(N'2022-04-12T08:10:47.0000000' AS DateTime2), 1, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (399, 8, CAST(N'2023-10-30T11:08:40.0000000' AS DateTime2), 12, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (400, 8, CAST(N'2025-05-18T14:05:52.0000000' AS DateTime2), 11, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (401, 8, CAST(N'2026-12-05T17:03:45.0000000' AS DateTime2), 10, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (402, 8, CAST(N'2028-06-23T20:00:56.0000000' AS DateTime2), 9, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (403, 8, CAST(N'2030-01-10T22:58:50.0000000' AS DateTime2), 8, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (404, 8, CAST(N'2031-07-31T01:56:43.0000000' AS DateTime2), 7, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (405, 8, CAST(N'2033-02-16T04:53:54.0000000' AS DateTime2), 6, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (406, 8, CAST(N'2034-09-05T07:51:48.0000000' AS DateTime2), 5, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (407, 8, CAST(N'2036-03-24T10:49:41.0000000' AS DateTime2), 4, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (408, 8, CAST(N'2037-10-11T13:47:35.0000000' AS DateTime2), 3, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (409, 8, CAST(N'2039-04-30T16:45:28.0000000' AS DateTime2), 2, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (410, 8, CAST(N'2040-11-16T19:42:39.0000000' AS DateTime2), 1, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (411, 8, CAST(N'2042-06-05T22:40:33.0000000' AS DateTime2), 12, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (412, 8, CAST(N'2043-12-24T01:38:26.0000000' AS DateTime2), 11, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (413, 8, CAST(N'2045-07-12T04:36:20.0000000' AS DateTime2), 10, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (414, 8, CAST(N'2047-01-29T07:34:13.0000000' AS DateTime2), 9, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (415, 8, CAST(N'2048-08-17T10:32:07.0000000' AS DateTime2), 8, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (416, 8, CAST(N'2050-03-06T13:30:00.0000000' AS DateTime2), 7, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (417, 8, CAST(N'2051-09-23T16:28:36.0000000' AS DateTime2), 6, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (418, 8, CAST(N'2053-04-11T19:26:29.0000000' AS DateTime2), 5, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (419, 8, CAST(N'2054-10-29T22:24:23.0000000' AS DateTime2), 4, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (420, 8, CAST(N'2056-05-18T01:22:16.0000000' AS DateTime2), 3, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (421, 8, CAST(N'2057-12-05T04:20:09.0000000' AS DateTime2), 2, N'Retrograde', 0, NULL)
INSERT [dbo].[tbl_PlanetSignTransitEvents] ([Id], [PlanetId], [EventDateTimeUtc], [SignId], [MotionDirection], [IsReentry], [Notes]) VALUES (422, 8, CAST(N'2059-06-24T07:18:45.0000000' AS DateTime2), 1, N'Retrograde', 0, NULL)
SET IDENTITY_INSERT [dbo].[tbl_PlanetSignTransitEvents] OFF
END
GO

-- --- tbl_Rule_Sets ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Sets)
BEGIN
INSERT [dbo].[tbl_Rule_Sets] ([Id], [RuleSetName], [Description], [IsActive]) VALUES (1, N'Parashari-Classical', N'This project''s existing hardcoded rules (ClassicalRelationships.cs / ClassicalCombustion.cs) as of 2026-08-30 -- Rahu/Ketu use Jupiter-style 5th/7th/9th aspects, BPHS/Phaladeepika combustion orbs.', 1)
END
GO

-- --- tbl_Rule_AspectOffset ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_AspectOffset)
BEGIN
SET IDENTITY_INSERT [dbo].[tbl_Rule_AspectOffset] ON 

INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (1, 1, 1, 7, N'7th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (2, 1, 2, 7, N'7th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (3, 1, 3, 4, N'4th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (4, 1, 3, 7, N'7th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (5, 1, 3, 8, N'8th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (6, 1, 4, 7, N'7th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (7, 1, 5, 5, N'5th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (8, 1, 5, 7, N'7th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (9, 1, 5, 9, N'9th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (10, 1, 6, 7, N'7th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (11, 1, 7, 3, N'3rd')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (12, 1, 7, 7, N'7th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (13, 1, 7, 10, N'10th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (14, 1, 8, 5, N'5th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (15, 1, 8, 7, N'7th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (16, 1, 8, 9, N'9th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (17, 1, 9, 5, N'5th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (18, 1, 9, 7, N'7th')
INSERT [dbo].[tbl_Rule_AspectOffset] ([Id], [RuleSetId], [PlanetId], [HouseOffset], [OffsetLabel]) VALUES (19, 1, 9, 9, N'9th')
SET IDENTITY_INSERT [dbo].[tbl_Rule_AspectOffset] OFF
END
GO

-- --- tbl_Rule_CombustionOrb ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_CombustionOrb)
BEGIN
SET IDENTITY_INSERT [dbo].[tbl_Rule_CombustionOrb] ON 

INSERT [dbo].[tbl_Rule_CombustionOrb] ([Id], [RuleSetId], [PlanetId], [DirectOrbDegrees], [RetrogradeOrbDegrees]) VALUES (1, 1, 2, CAST(12.00 AS Decimal(5, 2)), NULL)
INSERT [dbo].[tbl_Rule_CombustionOrb] ([Id], [RuleSetId], [PlanetId], [DirectOrbDegrees], [RetrogradeOrbDegrees]) VALUES (2, 1, 3, CAST(17.00 AS Decimal(5, 2)), CAST(8.00 AS Decimal(5, 2)))
INSERT [dbo].[tbl_Rule_CombustionOrb] ([Id], [RuleSetId], [PlanetId], [DirectOrbDegrees], [RetrogradeOrbDegrees]) VALUES (3, 1, 4, CAST(14.00 AS Decimal(5, 2)), CAST(12.00 AS Decimal(5, 2)))
INSERT [dbo].[tbl_Rule_CombustionOrb] ([Id], [RuleSetId], [PlanetId], [DirectOrbDegrees], [RetrogradeOrbDegrees]) VALUES (4, 1, 5, CAST(11.00 AS Decimal(5, 2)), NULL)
INSERT [dbo].[tbl_Rule_CombustionOrb] ([Id], [RuleSetId], [PlanetId], [DirectOrbDegrees], [RetrogradeOrbDegrees]) VALUES (5, 1, 6, CAST(10.00 AS Decimal(5, 2)), CAST(8.00 AS Decimal(5, 2)))
INSERT [dbo].[tbl_Rule_CombustionOrb] ([Id], [RuleSetId], [PlanetId], [DirectOrbDegrees], [RetrogradeOrbDegrees]) VALUES (6, 1, 7, CAST(15.00 AS Decimal(5, 2)), NULL)
SET IDENTITY_INSERT [dbo].[tbl_Rule_CombustionOrb] OFF
END
GO

-- --- tbl_Rule_NaturalRelationship ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_NaturalRelationship)
BEGIN
SET IDENTITY_INSERT [dbo].[tbl_Rule_NaturalRelationship] ON 

INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (1, 1, 1, 2, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (2, 1, 1, 3, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (3, 1, 1, 5, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (4, 1, 1, 4, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (5, 1, 1, 6, N'Enemy')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (6, 1, 1, 7, N'Enemy')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (7, 1, 2, 1, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (8, 1, 2, 4, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (9, 1, 2, 3, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (10, 1, 2, 5, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (11, 1, 2, 6, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (12, 1, 2, 7, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (13, 1, 3, 1, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (14, 1, 3, 2, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (15, 1, 3, 5, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (16, 1, 3, 6, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (17, 1, 3, 7, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (18, 1, 3, 4, N'Enemy')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (19, 1, 4, 1, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (20, 1, 4, 6, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (21, 1, 4, 3, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (22, 1, 4, 5, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (23, 1, 4, 7, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (24, 1, 4, 2, N'Enemy')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (25, 1, 5, 1, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (26, 1, 5, 2, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (27, 1, 5, 3, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (28, 1, 5, 7, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (29, 1, 5, 4, N'Enemy')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (30, 1, 5, 6, N'Enemy')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (31, 1, 6, 4, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (32, 1, 6, 7, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (33, 1, 6, 3, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (34, 1, 6, 5, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (35, 1, 6, 1, N'Enemy')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (36, 1, 6, 2, N'Enemy')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (37, 1, 7, 4, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (38, 1, 7, 6, N'Friend')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (39, 1, 7, 5, N'Neutral')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (40, 1, 7, 1, N'Enemy')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (41, 1, 7, 2, N'Enemy')
INSERT [dbo].[tbl_Rule_NaturalRelationship] ([Id], [RuleSetId], [PlanetId], [RelatedPlanetId], [RelationshipType]) VALUES (42, 1, 7, 3, N'Enemy')
SET IDENTITY_INSERT [dbo].[tbl_Rule_NaturalRelationship] OFF
END
GO

-- --- tbl_Rule_TemporaryFriendshipDistance ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_TemporaryFriendshipDistance)
BEGIN
INSERT [dbo].[tbl_Rule_TemporaryFriendshipDistance] ([RuleSetId], [SignDistance], [IsFriend]) VALUES (1, 1, 0)
INSERT [dbo].[tbl_Rule_TemporaryFriendshipDistance] ([RuleSetId], [SignDistance], [IsFriend]) VALUES (1, 2, 1)
INSERT [dbo].[tbl_Rule_TemporaryFriendshipDistance] ([RuleSetId], [SignDistance], [IsFriend]) VALUES (1, 3, 1)
INSERT [dbo].[tbl_Rule_TemporaryFriendshipDistance] ([RuleSetId], [SignDistance], [IsFriend]) VALUES (1, 4, 1)
INSERT [dbo].[tbl_Rule_TemporaryFriendshipDistance] ([RuleSetId], [SignDistance], [IsFriend]) VALUES (1, 5, 0)
INSERT [dbo].[tbl_Rule_TemporaryFriendshipDistance] ([RuleSetId], [SignDistance], [IsFriend]) VALUES (1, 6, 0)
INSERT [dbo].[tbl_Rule_TemporaryFriendshipDistance] ([RuleSetId], [SignDistance], [IsFriend]) VALUES (1, 7, 0)
INSERT [dbo].[tbl_Rule_TemporaryFriendshipDistance] ([RuleSetId], [SignDistance], [IsFriend]) VALUES (1, 8, 0)
INSERT [dbo].[tbl_Rule_TemporaryFriendshipDistance] ([RuleSetId], [SignDistance], [IsFriend]) VALUES (1, 9, 0)
INSERT [dbo].[tbl_Rule_TemporaryFriendshipDistance] ([RuleSetId], [SignDistance], [IsFriend]) VALUES (1, 10, 1)
INSERT [dbo].[tbl_Rule_TemporaryFriendshipDistance] ([RuleSetId], [SignDistance], [IsFriend]) VALUES (1, 11, 1)
INSERT [dbo].[tbl_Rule_TemporaryFriendshipDistance] ([RuleSetId], [SignDistance], [IsFriend]) VALUES (1, 12, 1)
END
GO

-- ---------------------------------------------------------------------------
-- tbl_Dim_LifeCalendar seed (regenerated, not dumped): 43,830 rows / ~121 yrs
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM dbo.tbl_Dim_LifeCalendar)
BEGIN
    ;WITH Days AS (
        SELECT 0 AS DayOffset
        UNION ALL
        SELECT DayOffset + 1 FROM Days WHERE DayOffset < 43829
    )
    INSERT INTO dbo.tbl_Dim_LifeCalendar
        (DayOffset, WeekNumber, WeekStartOffset, WeekEndOffset,
         MonthNumber, MonthStartOffset, MonthEndOffset,
         YearNumber, YearStartOffset, YearEndOffset)
    SELECT
        DayOffset,
        (DayOffset / 7)   + 1        AS WeekNumber,
        (DayOffset / 7)   * 7        AS WeekStartOffset,
        (DayOffset / 7)   * 7  + 6   AS WeekEndOffset,
        (DayOffset / 30)  + 1        AS MonthNumber,
        (DayOffset / 30)  * 30       AS MonthStartOffset,
        (DayOffset / 30)  * 30 + 29  AS MonthEndOffset,
        (DayOffset / 365) + 1        AS YearNumber,
        (DayOffset / 365) * 365      AS YearStartOffset,
        (DayOffset / 365) * 365 + 364 AS YearEndOffset
    FROM Days
    OPTION (MAXRECURSION 0);
END
GO

-- =====================================================================
-- tbl_Dim_ChartType — controlled vocabulary for ChartResults.ChartTypeId
-- (folded from db/02_create_dim_charttype.sql + db/10_seed_varga_charttypes.sql).
-- Seeds the 21 registered position charts (D1, D2, D2-US, D3..D60);
-- Vimshottari Dasha is not a chart type (see CalculationKind).
-- Ids 22..24 are reserved for Plan B (D81/D108/D144).
-- =====================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
    ( 1, 'D1',    'Rasi',              1,  'Varga',  1),
    ( 2, 'D2',    'Hora',              2,  'Varga',  2),
    ( 3, 'D6',    'Shashtamsa',        6,  'Varga',  3),
    ( 4, 'D9',    'Navamsa',           9,  'Varga',  4),
    ( 5, 'D10',   'Dasamsa',           10, 'Varga',  5),
    ( 6, 'D11',   'Rudramsa',          11, 'Varga',  6),
    ( 7, 'D2-US', 'Hora (Uma Shambu)', 2,  'Varga',  7),
    ( 8, 'D3',    'Drekkana',          3,  'Varga',  8),
    ( 9, 'D4',    'Chaturthamsa',      4,  'Varga',  9),
    (10, 'D5',    'Panchamsa',         5,  'Varga', 10),
    (11, 'D7',    'Saptamsa',          7,  'Varga', 11),
    (12, 'D8',    'Ashtamsa',          8,  'Varga', 12),
    (13, 'D12',   'Dwadasamsa',        12, 'Varga', 13),
    (14, 'D16',   'Shodasamsa',        16, 'Varga', 14),
    (15, 'D20',   'Vimsamsa',          20, 'Varga', 15),
    (16, 'D24',   'Siddhamsa',         24, 'Varga', 16),
    (17, 'D27',   'Nakshatramsa',      27, 'Varga', 17),
    (18, 'D30',   'Trimsamsa',         30, 'Varga', 18),
    (19, 'D40',   'Khavedamsa',        40, 'Varga', 19),
    (20, 'D45',   'Akshavedamsa',      45, 'Varga', 20),
    (21, 'D60',   'Shashtyamsa',       60, 'Varga', 21);
GO
-- =====================================================================
-- 15 — tbl_Dim_Source: the SRC_* citation registry (STANDARDS §M.4). Mirror of
-- docs/research/reference-sources.md. Every SourceRefCode column added in later
-- plans (rule tables, terminology) FKs here by Code. Idempotent.
-- (folded from db/15_create_dim_source.sql).
-- =====================================================================
IF OBJECT_ID('dbo.tbl_Dim_Source', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_Source (
        Id        INT IDENTITY(1,1) CONSTRAINT PK_Dim_Source PRIMARY KEY,
        Code      VARCHAR(40)   NOT NULL CONSTRAINT UQ_Dim_Source_Code UNIQUE,
        Title     NVARCHAR(200) NOT NULL,
        Author    NVARCHAR(120) NULL,
        Edition   NVARCHAR(80)  NULL,
        Tradition VARCHAR(40)   NULL,
        Notes     NVARCHAR(400) NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_Dim_Source_IsActive DEFAULT 1,
        CONSTRAINT CK_Dim_Source_Code CHECK (Code LIKE 'SRC[_]%')
    );
END
GO
-- Seed / re-seed (idempotent MERGE on Code)
;WITH src (Code, Title, Author, Edition, Tradition, Notes) AS (
    SELECT * FROM (VALUES
        ('SRC_BPHS',            N'Brihat Parashara Hora Shastra', N'Parāśara (attrib.)', NULL, 'Parasari', N'Umbrella; prefer a chapter-scoped code where known'),
        ('SRC_BPHS_26',         N'BPHS ch. 26 — Graha Drishti',   NULL, NULL, 'Parasari', N'7th full; Mars 4/8, Jupiter 5/9, Saturn 3/10'),
        ('SRC_BPHS_27',         N'BPHS ch. 27 — Shadbala',        NULL, NULL, 'Parasari', N'Strength engine (Plan 3); lookup tables need a cited edition'),
        ('SRC_BPHS_COMBUSTION', N'BPHS — Asta (combustion) orbs', NULL, NULL, 'Parasari', N'Moon 12, Mars 17/8R, Mercury 14/12R, Jupiter 11, Venus 10/8R, Saturn 15'),
        ('SRC_BPHS_AVASTHA',    N'BPHS — Baladi & Jagradadi avasthas', NULL, NULL, 'Parasari', N'Bala .25 / Kumara .50 / Yuva 1 / Vriddha .125 / Mrita 0'),
        ('SRC_PHALADEEPIKA',    N'Phaladeepika',                  N'Mantreshvara', NULL, 'classical', N'Combustion orb cross-check'),
        ('SRC_RAMAN_HTJH',      N'How to Judge a Horoscope (I–II)', N'B. V. Raman', NULL, 'Raman', N'House/Lagna significations, functional nature; OCR extract in BookExtracts'),
        ('SRC_RAMAN_HINDU_PREDICTIVE', N'Hindu Predictive Astrology', N'B. V. Raman', NULL, 'Raman', N'General'),
        ('SRC_PYJHORA',         N'PyJHora (source)',              N'pyjhora', N'_research/PyJHora', 'mixed', N'Varga formulae, special-lagna/upagraha algorithms; AGPL, vendored for reference'),
        ('SRC_JHORA',           N'Jagannatha Hora (desktop)',     N'P. V. R. Narasimha Rao', N'v8.x', 'mixed', N'Golden-record verification'),
        ('SRC_JHORA_EXPORT_RAMAKRISHNAN', N'JHora natal export — 1_Ramakrishnan', NULL, N'22 Apr 1981 05:30 Chennai', NULL, N'verify-vargas / verify-jaimini golden values; docs/artifacts/reference-charts/Rammy_Jagannatha.txt'),
        ('SRC_RATH_VARGA',      N'Vedic Astrology / varga methods', N'Sanjay Rath', NULL, 'Jaimini/SJC', N'D11 (Rudramsa), argala'),
        ('SRC_VEDASTRO',        N'VedAstro.Library',              N'(open source)', N'pre-2026-08-24', 'mixed', N'Historical — replaced by SwissEphNet; enum spellings inherited'),
        ('SRC_SWISSEPH',        N'Swiss Ephemeris / SwissEphNet', N'Astrodienst / port', N'SwissEphNet 2.8.0.2', 'astronomy', N'Moshier mode, Lahiri sidereal')
    ) v (Code, Title, Author, Edition, Tradition, Notes)
)
MERGE dbo.tbl_Dim_Source AS tgt
USING src ON tgt.Code = src.Code
WHEN MATCHED THEN UPDATE SET tgt.Title = src.Title, tgt.Author = src.Author,
    tgt.Edition = src.Edition, tgt.Tradition = src.Tradition, tgt.Notes = src.Notes
WHEN NOT MATCHED THEN INSERT (Code, Title, Author, Edition, Tradition, Notes)
    VALUES (src.Code, src.Title, src.Author, src.Edition, src.Tradition, src.Notes);
GO
-- =====================================================================
-- 17 — tbl_Astro_Terminology + tbl_Astro_TerminologyText: the bilingual
-- concept catalogue (engine reorg, Plan 1). tbl_Astro_Terminology is the
-- language-neutral registry of engine concepts keyed by a stable Code,
-- with a self-referencing ParentCode hierarchy. tbl_Astro_TerminologyText
-- holds the per-language (sa/en/ta) display + technical text, one row per
-- (concept, language, script). Schema-only; no rows seeded.
-- (folded from db/17_create_astro_terminology.sql).
-- =====================================================================
IF OBJECT_ID('dbo.tbl_Astro_Terminology','U') IS NULL
CREATE TABLE dbo.tbl_Astro_Terminology (
    TerminologyId   INT IDENTITY(1,1) CONSTRAINT PK_Astro_Terminology PRIMARY KEY,
    Category        VARCHAR(30)  NOT NULL,
    Code            VARCHAR(40)  NOT NULL CONSTRAINT UQ_Astro_Terminology_Code UNIQUE,
    ParentCode      VARCHAR(40)  NULL,
    EngineCode      VARCHAR(30)  NULL,
    NumericKey      INT          NULL,
    FormulaSummary  VARCHAR(300) NULL,
    DisplayOrder    INT          NOT NULL CONSTRAINT DF_Astro_Terminology_DisplayOrder DEFAULT 0,
    IsActive        BIT          NOT NULL CONSTRAINT DF_Astro_Terminology_IsActive DEFAULT 1,
    CONSTRAINT CK_Astro_Terminology_Category CHECK (Category IN (
        'Planet','Sign','House','Nakshatra','NakshatraPada','DivisionalChart','Karaka',
        'SpecialPoint','AvasthaState','DignityState','Relationship','StrengthComponent',
        'Dasha','Yoga','Ayanamsa','Concept')),
    CONSTRAINT FK_Astro_Terminology_Parent FOREIGN KEY (ParentCode)
        REFERENCES dbo.tbl_Astro_Terminology (Code)
);
GO
IF OBJECT_ID('dbo.tbl_Astro_TerminologyText','U') IS NULL
CREATE TABLE dbo.tbl_Astro_TerminologyText (
    TerminologyTextId    INT IDENTITY(1,1) CONSTRAINT PK_Astro_TerminologyText PRIMARY KEY,
    TerminologyId        INT           NOT NULL,
    LanguageCode         CHAR(2)       NOT NULL,
    Script               VARCHAR(8)    NOT NULL CONSTRAINT DF_Astro_TerminologyText_Script DEFAULT 'Latn',
    Name                 NVARCHAR(100) NOT NULL,
    TraditionalName      NVARCHAR(100) NULL,
    ShortDescription     NVARCHAR(400) NULL,
    TechnicalDefinition  NVARCHAR(MAX) NULL,
    CalculationMethod    NVARCHAR(MAX) NULL,
    SourceRefCode        VARCHAR(40)   NULL,
    CONSTRAINT FK_Astro_TerminologyText FOREIGN KEY (TerminologyId)
        REFERENCES dbo.tbl_Astro_Terminology (TerminologyId),
    CONSTRAINT CK_Astro_TerminologyText_Lang   CHECK (LanguageCode IN ('sa','en','ta')),
    CONSTRAINT CK_Astro_TerminologyText_Script CHECK (Script IN ('Latn','Deva','Taml')),
    CONSTRAINT CK_Astro_TerminologyText_Src    CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
    CONSTRAINT UQ_Astro_TerminologyText UNIQUE (TerminologyId, LanguageCode, Script)
);
GO
-- ---------------------------------------------------------------------
-- Terminology concept catalogue seed (sa + en, script Latn) - folded from
-- the seed-terminology CLI mode. Source of truth: the Core enums + Dim
-- tables (see src/Ikiastrro.Cli/TerminologySeed.cs). A fresh baseline
-- install gets the full bilingual catalogue with no CLI run. Idempotent
-- MERGE on Code / (TerminologyId, LanguageCode, Script).
-- regenerate via:
--   dotnet run --project src/Ikiastrro.Cli -- seed-terminology --emit-sql
--   (writes db/terminology-seed.generated.sql; replace the lines between
--    the BEGIN/END TERMINOLOGY SEED markers below with its contents)
-- ---------------------------------------------------------------------
-- >>> BEGIN TERMINOLOGY SEED (generated - do not hand-edit) >>>
-- dumped 2026-09-02 from db 'ikiastrro' - 236 concept rows / 472 text rows (sa+en, Latn).
MERGE dbo.tbl_Astro_Terminology AS tgt
USING (VALUES
  ('Planet','PLANET_SUN','ASTRO_CALC',0,1),
  ('Planet','PLANET_MOON','ASTRO_CALC',1,2),
  ('Planet','PLANET_MARS','ASTRO_CALC',2,3),
  ('Planet','PLANET_MERCURY','ASTRO_CALC',3,4),
  ('Planet','PLANET_JUPITER','ASTRO_CALC',4,5),
  ('Planet','PLANET_VENUS','ASTRO_CALC',5,6),
  ('Planet','PLANET_SATURN','ASTRO_CALC',6,7),
  ('Planet','PLANET_RAHU','ASTRO_CALC',7,8),
  ('Planet','PLANET_KETU','ASTRO_CALC',8,9),
  ('Sign','SIGN_ARIES','ASTRO_CALC',0,10),
  ('Sign','SIGN_TAURUS','ASTRO_CALC',1,11),
  ('Sign','SIGN_GEMINI','ASTRO_CALC',2,12),
  ('Sign','SIGN_CANCER','ASTRO_CALC',3,13),
  ('Sign','SIGN_LEO','ASTRO_CALC',4,14),
  ('Sign','SIGN_VIRGO','ASTRO_CALC',5,15),
  ('Sign','SIGN_LIBRA','ASTRO_CALC',6,16),
  ('Sign','SIGN_SCORPIO','ASTRO_CALC',7,17),
  ('Sign','SIGN_SAGITTARIUS','ASTRO_CALC',8,18),
  ('Sign','SIGN_CAPRICORN','ASTRO_CALC',9,19),
  ('Sign','SIGN_AQUARIUS','ASTRO_CALC',10,20),
  ('Sign','SIGN_PISCES','ASTRO_CALC',11,21),
  ('House','HOUSE_01','HOUSE',1,22),
  ('House','HOUSE_02','HOUSE',2,23),
  ('House','HOUSE_03','HOUSE',3,24),
  ('House','HOUSE_04','HOUSE',4,25),
  ('House','HOUSE_05','HOUSE',5,26),
  ('House','HOUSE_06','HOUSE',6,27),
  ('House','HOUSE_07','HOUSE',7,28),
  ('House','HOUSE_08','HOUSE',8,29),
  ('House','HOUSE_09','HOUSE',9,30),
  ('House','HOUSE_10','HOUSE',10,31),
  ('House','HOUSE_11','HOUSE',11,32),
  ('House','HOUSE_12','HOUSE',12,33),
  ('Nakshatra','NAK_ASHWINI','NAKSHATRA',0,34),
  ('Nakshatra','NAK_BHARANI','NAKSHATRA',1,35),
  ('Nakshatra','NAK_KRITTIKA','NAKSHATRA',2,36),
  ('Nakshatra','NAK_ROHINI','NAKSHATRA',3,37),
  ('Nakshatra','NAK_MRIGASHIRA','NAKSHATRA',4,38),
  ('Nakshatra','NAK_ARDRA','NAKSHATRA',5,39),
  ('Nakshatra','NAK_PUNARVASU','NAKSHATRA',6,40),
  ('Nakshatra','NAK_PUSHYA','NAKSHATRA',7,41),
  ('Nakshatra','NAK_ASHLESHA','NAKSHATRA',8,42),
  ('Nakshatra','NAK_MAGHA','NAKSHATRA',9,43),
  ('Nakshatra','NAK_PURVA_PHALGUNI','NAKSHATRA',10,44),
  ('Nakshatra','NAK_UTTARA_PHALGUNI','NAKSHATRA',11,45),
  ('Nakshatra','NAK_HASTA','NAKSHATRA',12,46),
  ('Nakshatra','NAK_CHITRA','NAKSHATRA',13,47),
  ('Nakshatra','NAK_SWATI','NAKSHATRA',14,48),
  ('Nakshatra','NAK_VISHAKHA','NAKSHATRA',15,49),
  ('Nakshatra','NAK_ANURADHA','NAKSHATRA',16,50),
  ('Nakshatra','NAK_JYESHTHA','NAKSHATRA',17,51),
  ('Nakshatra','NAK_MULA','NAKSHATRA',18,52),
  ('Nakshatra','NAK_PURVA_ASHADHA','NAKSHATRA',19,53),
  ('Nakshatra','NAK_UTTARA_ASHADHA','NAKSHATRA',20,54),
  ('Nakshatra','NAK_SHRAVANA','NAKSHATRA',21,55),
  ('Nakshatra','NAK_DHANISHTA','NAKSHATRA',22,56),
  ('Nakshatra','NAK_SHATABHISHA','NAKSHATRA',23,57),
  ('Nakshatra','NAK_PURVA_BHADRAPADA','NAKSHATRA',24,58),
  ('Nakshatra','NAK_UTTARA_BHADRAPADA','NAKSHATRA',25,59),
  ('Nakshatra','NAK_REVATI','NAKSHATRA',26,60),
  ('DivisionalChart','VARGA_D1','VARGA',1,169),
  ('DivisionalChart','VARGA_D2','VARGA',2,170),
  ('DivisionalChart','VARGA_D6','VARGA',3,171),
  ('DivisionalChart','VARGA_D9','VARGA',4,172),
  ('DivisionalChart','VARGA_D10','VARGA',5,173),
  ('DivisionalChart','VARGA_D11','VARGA',6,174),
  ('DivisionalChart','VARGA_D2_US','VARGA',7,175),
  ('DivisionalChart','VARGA_D3','VARGA',8,176),
  ('DivisionalChart','VARGA_D4','VARGA',9,177),
  ('DivisionalChart','VARGA_D5','VARGA',10,178),
  ('DivisionalChart','VARGA_D7','VARGA',11,179),
  ('DivisionalChart','VARGA_D8','VARGA',12,180),
  ('DivisionalChart','VARGA_D12','VARGA',13,181),
  ('DivisionalChart','VARGA_D16','VARGA',14,182),
  ('DivisionalChart','VARGA_D20','VARGA',15,183),
  ('DivisionalChart','VARGA_D24','VARGA',16,184),
  ('DivisionalChart','VARGA_D27','VARGA',17,185),
  ('DivisionalChart','VARGA_D30','VARGA',18,186),
  ('DivisionalChart','VARGA_D40','VARGA',19,187),
  ('DivisionalChart','VARGA_D45','VARGA',20,188),
  ('DivisionalChart','VARGA_D60','VARGA',21,189),
  ('Karaka','KARAKA_AK','KARAKA',0,190),
  ('Karaka','KARAKA_AMK','KARAKA',1,191),
  ('Karaka','KARAKA_BK','KARAKA',2,192),
  ('Karaka','KARAKA_MK','KARAKA',3,193),
  ('Karaka','KARAKA_PIK','KARAKA',4,194),
  ('Karaka','KARAKA_PK','KARAKA',5,195),
  ('Karaka','KARAKA_GK','KARAKA',6,196),
  ('Karaka','KARAKA_DK','KARAKA',7,197),
  ('SpecialPoint','SPT_AL','KARAKA',NULL,198),
  ('SpecialPoint','SPT_A2','KARAKA',NULL,199),
  ('SpecialPoint','SPT_A3','KARAKA',NULL,200),
  ('SpecialPoint','SPT_A4','KARAKA',NULL,201),
  ('SpecialPoint','SPT_A5','KARAKA',NULL,202),
  ('SpecialPoint','SPT_A6','KARAKA',NULL,203),
  ('SpecialPoint','SPT_A7','KARAKA',NULL,204),
  ('SpecialPoint','SPT_A8','KARAKA',NULL,205),
  ('SpecialPoint','SPT_A9','KARAKA',NULL,206),
  ('SpecialPoint','SPT_A10','KARAKA',NULL,207),
  ('SpecialPoint','SPT_A11','KARAKA',NULL,208),
  ('SpecialPoint','SPT_A12','KARAKA',NULL,209),
  ('SpecialPoint','SPT_HL','KARAKA',NULL,210),
  ('SpecialPoint','SPT_GULIKA','KARAKA',NULL,211),
  ('SpecialPoint','SPT_MAANDI','KARAKA',NULL,212),
  ('AvasthaState','AVASTHA_BAALADI_BAALA','AVASTHA',1,213),
  ('AvasthaState','AVASTHA_BAALADI_KUMARA','AVASTHA',2,214),
  ('AvasthaState','AVASTHA_BAALADI_YUVA','AVASTHA',3,215),
  ('AvasthaState','AVASTHA_BAALADI_VRIDDHA','AVASTHA',4,216),
  ('AvasthaState','AVASTHA_BAALADI_MRITA','AVASTHA',5,217),
  ('AvasthaState','AVASTHA_JAGRADADI_JAGRAT','AVASTHA',6,218),
  ('AvasthaState','AVASTHA_JAGRADADI_SWAPNA','AVASTHA',7,219),
  ('AvasthaState','AVASTHA_JAGRADADI_SUSHUPTI','AVASTHA',8,220),
  ('DignityState','DIGNITY_EXALTED','DIGNITY',1,221),
  ('DignityState','DIGNITY_OWN_SIGN','DIGNITY',2,222),
  ('DignityState','DIGNITY_MOOLATRIKONA','DIGNITY',3,223),
  ('DignityState','DIGNITY_GREAT_FRIEND','DIGNITY',4,224),
  ('DignityState','DIGNITY_FRIEND','DIGNITY',5,225),
  ('DignityState','DIGNITY_NEUTRAL','DIGNITY',6,226),
  ('DignityState','DIGNITY_ENEMY','DIGNITY',7,227),
  ('DignityState','DIGNITY_GREAT_ENEMY','DIGNITY',8,228),
  ('DignityState','DIGNITY_DEBILITATED','DIGNITY',9,229),
  ('Relationship','REL_YUTI','RELATIONSHIP',NULL,230),
  ('Relationship','REL_DRISHTI','RELATIONSHIP',NULL,231),
  ('Relationship','REL_COMBUST','RELATIONSHIP',NULL,232),
  ('Relationship','REL_GREAT_FRIEND','RELATIONSHIP',NULL,233),
  ('Relationship','REL_FRIEND','RELATIONSHIP',NULL,234),
  ('Relationship','REL_ENEMY','RELATIONSHIP',NULL,235),
  ('Ayanamsa','AYANAMSA_LAHIRI','ASTRO_CALC',1,236)
) AS src (Category, Code, EngineCode, NumericKey, DisplayOrder)
ON tgt.Code = src.Code
WHEN MATCHED THEN UPDATE SET Category = src.Category, EngineCode = src.EngineCode,
    NumericKey = src.NumericKey, DisplayOrder = src.DisplayOrder, IsActive = 1
WHEN NOT MATCHED THEN INSERT (Category, Code, EngineCode, NumericKey, DisplayOrder, IsActive)
    VALUES (src.Category, src.Code, src.EngineCode, src.NumericKey, src.DisplayOrder, 1);
GO
MERGE dbo.tbl_Astro_Terminology AS tgt
USING (VALUES
  ('NakshatraPada','NAKPADA_ASHWINI_1','NAK_ASHWINI','NAKSHATRA',1,61),
  ('NakshatraPada','NAKPADA_ASHWINI_2','NAK_ASHWINI','NAKSHATRA',2,62),
  ('NakshatraPada','NAKPADA_ASHWINI_3','NAK_ASHWINI','NAKSHATRA',3,63),
  ('NakshatraPada','NAKPADA_ASHWINI_4','NAK_ASHWINI','NAKSHATRA',4,64),
  ('NakshatraPada','NAKPADA_BHARANI_1','NAK_BHARANI','NAKSHATRA',1,65),
  ('NakshatraPada','NAKPADA_BHARANI_2','NAK_BHARANI','NAKSHATRA',2,66),
  ('NakshatraPada','NAKPADA_BHARANI_3','NAK_BHARANI','NAKSHATRA',3,67),
  ('NakshatraPada','NAKPADA_BHARANI_4','NAK_BHARANI','NAKSHATRA',4,68),
  ('NakshatraPada','NAKPADA_KRITTIKA_1','NAK_KRITTIKA','NAKSHATRA',1,69),
  ('NakshatraPada','NAKPADA_KRITTIKA_2','NAK_KRITTIKA','NAKSHATRA',2,70),
  ('NakshatraPada','NAKPADA_KRITTIKA_3','NAK_KRITTIKA','NAKSHATRA',3,71),
  ('NakshatraPada','NAKPADA_KRITTIKA_4','NAK_KRITTIKA','NAKSHATRA',4,72),
  ('NakshatraPada','NAKPADA_ROHINI_1','NAK_ROHINI','NAKSHATRA',1,73),
  ('NakshatraPada','NAKPADA_ROHINI_2','NAK_ROHINI','NAKSHATRA',2,74),
  ('NakshatraPada','NAKPADA_ROHINI_3','NAK_ROHINI','NAKSHATRA',3,75),
  ('NakshatraPada','NAKPADA_ROHINI_4','NAK_ROHINI','NAKSHATRA',4,76),
  ('NakshatraPada','NAKPADA_MRIGASHIRA_1','NAK_MRIGASHIRA','NAKSHATRA',1,77),
  ('NakshatraPada','NAKPADA_MRIGASHIRA_2','NAK_MRIGASHIRA','NAKSHATRA',2,78),
  ('NakshatraPada','NAKPADA_MRIGASHIRA_3','NAK_MRIGASHIRA','NAKSHATRA',3,79),
  ('NakshatraPada','NAKPADA_MRIGASHIRA_4','NAK_MRIGASHIRA','NAKSHATRA',4,80),
  ('NakshatraPada','NAKPADA_ARDRA_1','NAK_ARDRA','NAKSHATRA',1,81),
  ('NakshatraPada','NAKPADA_ARDRA_2','NAK_ARDRA','NAKSHATRA',2,82),
  ('NakshatraPada','NAKPADA_ARDRA_3','NAK_ARDRA','NAKSHATRA',3,83),
  ('NakshatraPada','NAKPADA_ARDRA_4','NAK_ARDRA','NAKSHATRA',4,84),
  ('NakshatraPada','NAKPADA_PUNARVASU_1','NAK_PUNARVASU','NAKSHATRA',1,85),
  ('NakshatraPada','NAKPADA_PUNARVASU_2','NAK_PUNARVASU','NAKSHATRA',2,86),
  ('NakshatraPada','NAKPADA_PUNARVASU_3','NAK_PUNARVASU','NAKSHATRA',3,87),
  ('NakshatraPada','NAKPADA_PUNARVASU_4','NAK_PUNARVASU','NAKSHATRA',4,88),
  ('NakshatraPada','NAKPADA_PUSHYA_1','NAK_PUSHYA','NAKSHATRA',1,89),
  ('NakshatraPada','NAKPADA_PUSHYA_2','NAK_PUSHYA','NAKSHATRA',2,90),
  ('NakshatraPada','NAKPADA_PUSHYA_3','NAK_PUSHYA','NAKSHATRA',3,91),
  ('NakshatraPada','NAKPADA_PUSHYA_4','NAK_PUSHYA','NAKSHATRA',4,92),
  ('NakshatraPada','NAKPADA_ASHLESHA_1','NAK_ASHLESHA','NAKSHATRA',1,93),
  ('NakshatraPada','NAKPADA_ASHLESHA_2','NAK_ASHLESHA','NAKSHATRA',2,94),
  ('NakshatraPada','NAKPADA_ASHLESHA_3','NAK_ASHLESHA','NAKSHATRA',3,95),
  ('NakshatraPada','NAKPADA_ASHLESHA_4','NAK_ASHLESHA','NAKSHATRA',4,96),
  ('NakshatraPada','NAKPADA_MAGHA_1','NAK_MAGHA','NAKSHATRA',1,97),
  ('NakshatraPada','NAKPADA_MAGHA_2','NAK_MAGHA','NAKSHATRA',2,98),
  ('NakshatraPada','NAKPADA_MAGHA_3','NAK_MAGHA','NAKSHATRA',3,99),
  ('NakshatraPada','NAKPADA_MAGHA_4','NAK_MAGHA','NAKSHATRA',4,100),
  ('NakshatraPada','NAKPADA_PURVA_PHALGUNI_1','NAK_PURVA_PHALGUNI','NAKSHATRA',1,101),
  ('NakshatraPada','NAKPADA_PURVA_PHALGUNI_2','NAK_PURVA_PHALGUNI','NAKSHATRA',2,102),
  ('NakshatraPada','NAKPADA_PURVA_PHALGUNI_3','NAK_PURVA_PHALGUNI','NAKSHATRA',3,103),
  ('NakshatraPada','NAKPADA_PURVA_PHALGUNI_4','NAK_PURVA_PHALGUNI','NAKSHATRA',4,104),
  ('NakshatraPada','NAKPADA_UTTARA_PHALGUNI_1','NAK_UTTARA_PHALGUNI','NAKSHATRA',1,105),
  ('NakshatraPada','NAKPADA_UTTARA_PHALGUNI_2','NAK_UTTARA_PHALGUNI','NAKSHATRA',2,106),
  ('NakshatraPada','NAKPADA_UTTARA_PHALGUNI_3','NAK_UTTARA_PHALGUNI','NAKSHATRA',3,107),
  ('NakshatraPada','NAKPADA_UTTARA_PHALGUNI_4','NAK_UTTARA_PHALGUNI','NAKSHATRA',4,108),
  ('NakshatraPada','NAKPADA_HASTA_1','NAK_HASTA','NAKSHATRA',1,109),
  ('NakshatraPada','NAKPADA_HASTA_2','NAK_HASTA','NAKSHATRA',2,110),
  ('NakshatraPada','NAKPADA_HASTA_3','NAK_HASTA','NAKSHATRA',3,111),
  ('NakshatraPada','NAKPADA_HASTA_4','NAK_HASTA','NAKSHATRA',4,112),
  ('NakshatraPada','NAKPADA_CHITRA_1','NAK_CHITRA','NAKSHATRA',1,113),
  ('NakshatraPada','NAKPADA_CHITRA_2','NAK_CHITRA','NAKSHATRA',2,114),
  ('NakshatraPada','NAKPADA_CHITRA_3','NAK_CHITRA','NAKSHATRA',3,115),
  ('NakshatraPada','NAKPADA_CHITRA_4','NAK_CHITRA','NAKSHATRA',4,116),
  ('NakshatraPada','NAKPADA_SWATI_1','NAK_SWATI','NAKSHATRA',1,117),
  ('NakshatraPada','NAKPADA_SWATI_2','NAK_SWATI','NAKSHATRA',2,118),
  ('NakshatraPada','NAKPADA_SWATI_3','NAK_SWATI','NAKSHATRA',3,119),
  ('NakshatraPada','NAKPADA_SWATI_4','NAK_SWATI','NAKSHATRA',4,120),
  ('NakshatraPada','NAKPADA_VISHAKHA_1','NAK_VISHAKHA','NAKSHATRA',1,121),
  ('NakshatraPada','NAKPADA_VISHAKHA_2','NAK_VISHAKHA','NAKSHATRA',2,122),
  ('NakshatraPada','NAKPADA_VISHAKHA_3','NAK_VISHAKHA','NAKSHATRA',3,123),
  ('NakshatraPada','NAKPADA_VISHAKHA_4','NAK_VISHAKHA','NAKSHATRA',4,124),
  ('NakshatraPada','NAKPADA_ANURADHA_1','NAK_ANURADHA','NAKSHATRA',1,125),
  ('NakshatraPada','NAKPADA_ANURADHA_2','NAK_ANURADHA','NAKSHATRA',2,126),
  ('NakshatraPada','NAKPADA_ANURADHA_3','NAK_ANURADHA','NAKSHATRA',3,127),
  ('NakshatraPada','NAKPADA_ANURADHA_4','NAK_ANURADHA','NAKSHATRA',4,128),
  ('NakshatraPada','NAKPADA_JYESHTHA_1','NAK_JYESHTHA','NAKSHATRA',1,129),
  ('NakshatraPada','NAKPADA_JYESHTHA_2','NAK_JYESHTHA','NAKSHATRA',2,130),
  ('NakshatraPada','NAKPADA_JYESHTHA_3','NAK_JYESHTHA','NAKSHATRA',3,131),
  ('NakshatraPada','NAKPADA_JYESHTHA_4','NAK_JYESHTHA','NAKSHATRA',4,132),
  ('NakshatraPada','NAKPADA_MULA_1','NAK_MULA','NAKSHATRA',1,133),
  ('NakshatraPada','NAKPADA_MULA_2','NAK_MULA','NAKSHATRA',2,134),
  ('NakshatraPada','NAKPADA_MULA_3','NAK_MULA','NAKSHATRA',3,135),
  ('NakshatraPada','NAKPADA_MULA_4','NAK_MULA','NAKSHATRA',4,136),
  ('NakshatraPada','NAKPADA_PURVA_ASHADHA_1','NAK_PURVA_ASHADHA','NAKSHATRA',1,137),
  ('NakshatraPada','NAKPADA_PURVA_ASHADHA_2','NAK_PURVA_ASHADHA','NAKSHATRA',2,138),
  ('NakshatraPada','NAKPADA_PURVA_ASHADHA_3','NAK_PURVA_ASHADHA','NAKSHATRA',3,139),
  ('NakshatraPada','NAKPADA_PURVA_ASHADHA_4','NAK_PURVA_ASHADHA','NAKSHATRA',4,140),
  ('NakshatraPada','NAKPADA_UTTARA_ASHADHA_1','NAK_UTTARA_ASHADHA','NAKSHATRA',1,141),
  ('NakshatraPada','NAKPADA_UTTARA_ASHADHA_2','NAK_UTTARA_ASHADHA','NAKSHATRA',2,142),
  ('NakshatraPada','NAKPADA_UTTARA_ASHADHA_3','NAK_UTTARA_ASHADHA','NAKSHATRA',3,143),
  ('NakshatraPada','NAKPADA_UTTARA_ASHADHA_4','NAK_UTTARA_ASHADHA','NAKSHATRA',4,144),
  ('NakshatraPada','NAKPADA_SHRAVANA_1','NAK_SHRAVANA','NAKSHATRA',1,145),
  ('NakshatraPada','NAKPADA_SHRAVANA_2','NAK_SHRAVANA','NAKSHATRA',2,146),
  ('NakshatraPada','NAKPADA_SHRAVANA_3','NAK_SHRAVANA','NAKSHATRA',3,147),
  ('NakshatraPada','NAKPADA_SHRAVANA_4','NAK_SHRAVANA','NAKSHATRA',4,148),
  ('NakshatraPada','NAKPADA_DHANISHTA_1','NAK_DHANISHTA','NAKSHATRA',1,149),
  ('NakshatraPada','NAKPADA_DHANISHTA_2','NAK_DHANISHTA','NAKSHATRA',2,150),
  ('NakshatraPada','NAKPADA_DHANISHTA_3','NAK_DHANISHTA','NAKSHATRA',3,151),
  ('NakshatraPada','NAKPADA_DHANISHTA_4','NAK_DHANISHTA','NAKSHATRA',4,152),
  ('NakshatraPada','NAKPADA_SHATABHISHA_1','NAK_SHATABHISHA','NAKSHATRA',1,153),
  ('NakshatraPada','NAKPADA_SHATABHISHA_2','NAK_SHATABHISHA','NAKSHATRA',2,154),
  ('NakshatraPada','NAKPADA_SHATABHISHA_3','NAK_SHATABHISHA','NAKSHATRA',3,155),
  ('NakshatraPada','NAKPADA_SHATABHISHA_4','NAK_SHATABHISHA','NAKSHATRA',4,156),
  ('NakshatraPada','NAKPADA_PURVA_BHADRAPADA_1','NAK_PURVA_BHADRAPADA','NAKSHATRA',1,157),
  ('NakshatraPada','NAKPADA_PURVA_BHADRAPADA_2','NAK_PURVA_BHADRAPADA','NAKSHATRA',2,158),
  ('NakshatraPada','NAKPADA_PURVA_BHADRAPADA_3','NAK_PURVA_BHADRAPADA','NAKSHATRA',3,159),
  ('NakshatraPada','NAKPADA_PURVA_BHADRAPADA_4','NAK_PURVA_BHADRAPADA','NAKSHATRA',4,160),
  ('NakshatraPada','NAKPADA_UTTARA_BHADRAPADA_1','NAK_UTTARA_BHADRAPADA','NAKSHATRA',1,161),
  ('NakshatraPada','NAKPADA_UTTARA_BHADRAPADA_2','NAK_UTTARA_BHADRAPADA','NAKSHATRA',2,162),
  ('NakshatraPada','NAKPADA_UTTARA_BHADRAPADA_3','NAK_UTTARA_BHADRAPADA','NAKSHATRA',3,163),
  ('NakshatraPada','NAKPADA_UTTARA_BHADRAPADA_4','NAK_UTTARA_BHADRAPADA','NAKSHATRA',4,164),
  ('NakshatraPada','NAKPADA_REVATI_1','NAK_REVATI','NAKSHATRA',1,165),
  ('NakshatraPada','NAKPADA_REVATI_2','NAK_REVATI','NAKSHATRA',2,166),
  ('NakshatraPada','NAKPADA_REVATI_3','NAK_REVATI','NAKSHATRA',3,167),
  ('NakshatraPada','NAKPADA_REVATI_4','NAK_REVATI','NAKSHATRA',4,168)
) AS src (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder)
ON tgt.Code = src.Code
WHEN MATCHED THEN UPDATE SET Category = src.Category, ParentCode = src.ParentCode,
    EngineCode = src.EngineCode, NumericKey = src.NumericKey, DisplayOrder = src.DisplayOrder, IsActive = 1
WHEN NOT MATCHED THEN INSERT (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder, IsActive)
    VALUES (src.Category, src.Code, src.ParentCode, src.EngineCode, src.NumericKey, src.DisplayOrder, 1);
GO
MERGE dbo.tbl_Astro_TerminologyText AS tgt
USING (
  SELECT t.TerminologyId, v.LanguageCode, v.Script, v.Name, v.TraditionalName, v.ShortDescription
  FROM (VALUES
   ('PLANET_SUN','sa','Latn',N'Surya',N'Surya',NULL),
   ('PLANET_SUN','en','Latn',N'Sun',NULL,N'The graha Sun (Surya).'),
   ('PLANET_MOON','sa','Latn',N'Chandra',N'Chandra',NULL),
   ('PLANET_MOON','en','Latn',N'Moon',NULL,N'The graha Moon (Chandra).'),
   ('PLANET_MARS','sa','Latn',N'Mangala',N'Mangala',NULL),
   ('PLANET_MARS','en','Latn',N'Mars',NULL,N'The graha Mars (Mangala).'),
   ('PLANET_MERCURY','sa','Latn',N'Budha',N'Budha',NULL),
   ('PLANET_MERCURY','en','Latn',N'Mercury',NULL,N'The graha Mercury (Budha).'),
   ('PLANET_JUPITER','sa','Latn',N'Guru',N'Guru',NULL),
   ('PLANET_JUPITER','en','Latn',N'Jupiter',NULL,N'The graha Jupiter (Guru).'),
   ('PLANET_VENUS','sa','Latn',N'Shukra',N'Shukra',NULL),
   ('PLANET_VENUS','en','Latn',N'Venus',NULL,N'The graha Venus (Shukra).'),
   ('PLANET_SATURN','sa','Latn',N'Shani',N'Shani',NULL),
   ('PLANET_SATURN','en','Latn',N'Saturn',NULL,N'The graha Saturn (Shani).'),
   ('PLANET_RAHU','sa','Latn',N'Rahu',N'Rahu',NULL),
   ('PLANET_RAHU','en','Latn',N'Rahu',NULL,N'The graha Rahu (Rahu).'),
   ('PLANET_KETU','sa','Latn',N'Ketu',N'Ketu',NULL),
   ('PLANET_KETU','en','Latn',N'Ketu',NULL,N'The graha Ketu (Ketu).'),
   ('SIGN_ARIES','sa','Latn',N'Mesha',N'Mesha',NULL),
   ('SIGN_ARIES','en','Latn',N'Aries',NULL,N'Rasi 1 of 12 (Mesha).'),
   ('SIGN_TAURUS','sa','Latn',N'Vrishabha',N'Vrishabha',NULL),
   ('SIGN_TAURUS','en','Latn',N'Taurus',NULL,N'Rasi 2 of 12 (Vrishabha).'),
   ('SIGN_GEMINI','sa','Latn',N'Mithuna',N'Mithuna',NULL),
   ('SIGN_GEMINI','en','Latn',N'Gemini',NULL,N'Rasi 3 of 12 (Mithuna).'),
   ('SIGN_CANCER','sa','Latn',N'Karka',N'Karka',NULL),
   ('SIGN_CANCER','en','Latn',N'Cancer',NULL,N'Rasi 4 of 12 (Karka).'),
   ('SIGN_LEO','sa','Latn',N'Simha',N'Simha',NULL),
   ('SIGN_LEO','en','Latn',N'Leo',NULL,N'Rasi 5 of 12 (Simha).'),
   ('SIGN_VIRGO','sa','Latn',N'Kanya',N'Kanya',NULL),
   ('SIGN_VIRGO','en','Latn',N'Virgo',NULL,N'Rasi 6 of 12 (Kanya).'),
   ('SIGN_LIBRA','sa','Latn',N'Tula',N'Tula',NULL),
   ('SIGN_LIBRA','en','Latn',N'Libra',NULL,N'Rasi 7 of 12 (Tula).'),
   ('SIGN_SCORPIO','sa','Latn',N'Vrishchika',N'Vrishchika',NULL),
   ('SIGN_SCORPIO','en','Latn',N'Scorpio',NULL,N'Rasi 8 of 12 (Vrishchika).'),
   ('SIGN_SAGITTARIUS','sa','Latn',N'Dhanu',N'Dhanu',NULL),
   ('SIGN_SAGITTARIUS','en','Latn',N'Sagittarius',NULL,N'Rasi 9 of 12 (Dhanu).'),
   ('SIGN_CAPRICORN','sa','Latn',N'Makara',N'Makara',NULL),
   ('SIGN_CAPRICORN','en','Latn',N'Capricorn',NULL,N'Rasi 10 of 12 (Makara).'),
   ('SIGN_AQUARIUS','sa','Latn',N'Kumbha',N'Kumbha',NULL),
   ('SIGN_AQUARIUS','en','Latn',N'Aquarius',NULL,N'Rasi 11 of 12 (Kumbha).'),
   ('SIGN_PISCES','sa','Latn',N'Meena',N'Meena',NULL),
   ('SIGN_PISCES','en','Latn',N'Pisces',NULL,N'Rasi 12 of 12 (Meena).'),
   ('HOUSE_01','sa','Latn',N'Tanu Bhava',N'Tanu Bhava',NULL),
   ('HOUSE_01','en','Latn',N'1st house',NULL,N'Self, body and overall personality.'),
   ('HOUSE_02','sa','Latn',N'Dhana Bhava',N'Dhana Bhava',NULL),
   ('HOUSE_02','en','Latn',N'2nd house',NULL,N'Wealth, family and speech.'),
   ('HOUSE_03','sa','Latn',N'Sahaja Bhava',N'Sahaja Bhava',NULL),
   ('HOUSE_03','en','Latn',N'3rd house',NULL,N'Siblings, courage and initiative.'),
   ('HOUSE_04','sa','Latn',N'Bandhu Bhava',N'Bandhu Bhava',NULL),
   ('HOUSE_04','en','Latn',N'4th house',NULL,N'Home, mother and inner comfort.'),
   ('HOUSE_05','sa','Latn',N'Putra Bhava',N'Putra Bhava',NULL),
   ('HOUSE_05','en','Latn',N'5th house',NULL,N'Children, intellect and creativity.'),
   ('HOUSE_06','sa','Latn',N'Ari Bhava',N'Ari Bhava',NULL),
   ('HOUSE_06','en','Latn',N'6th house',NULL,N'Enemies, debt and disease.'),
   ('HOUSE_07','sa','Latn',N'Yuvati Bhava',N'Yuvati Bhava',NULL),
   ('HOUSE_07','en','Latn',N'7th house',NULL,N'Spouse and partnership.'),
   ('HOUSE_08','sa','Latn',N'Randhra Bhava',N'Randhra Bhava',NULL),
   ('HOUSE_08','en','Latn',N'8th house',NULL,N'Longevity, upheaval and hidden matters.'),
   ('HOUSE_09','sa','Latn',N'Dharma Bhava',N'Dharma Bhava',NULL),
   ('HOUSE_09','en','Latn',N'9th house',NULL,N'Fortune, dharma and father.'),
   ('HOUSE_10','sa','Latn',N'Karma Bhava',N'Karma Bhava',NULL),
   ('HOUSE_10','en','Latn',N'10th house',NULL,N'Career, status and public action.'),
   ('HOUSE_11','sa','Latn',N'Labha Bhava',N'Labha Bhava',NULL),
   ('HOUSE_11','en','Latn',N'11th house',NULL,N'Gains, income and social networks.'),
   ('HOUSE_12','sa','Latn',N'Vyaya Bhava',N'Vyaya Bhava',NULL),
   ('HOUSE_12','en','Latn',N'12th house',NULL,N'Loss, expense and liberation.'),
   ('NAK_ASHWINI','sa','Latn',N'Ashwini',N'Ashwini',NULL),
   ('NAK_ASHWINI','en','Latn',N'Ashwini',NULL,N'Nakshatra 1 of 27.'),
   ('NAK_BHARANI','sa','Latn',N'Bharani',N'Bharani',NULL),
   ('NAK_BHARANI','en','Latn',N'Bharani',NULL,N'Nakshatra 2 of 27.'),
   ('NAK_KRITTIKA','sa','Latn',N'Krittika',N'Krittika',NULL),
   ('NAK_KRITTIKA','en','Latn',N'Krittika',NULL,N'Nakshatra 3 of 27.'),
   ('NAK_ROHINI','sa','Latn',N'Rohini',N'Rohini',NULL),
   ('NAK_ROHINI','en','Latn',N'Rohini',NULL,N'Nakshatra 4 of 27.'),
   ('NAK_MRIGASHIRA','sa','Latn',N'Mrigashira',N'Mrigashira',NULL),
   ('NAK_MRIGASHIRA','en','Latn',N'Mrigashira',NULL,N'Nakshatra 5 of 27.'),
   ('NAK_ARDRA','sa','Latn',N'Ardra',N'Ardra',NULL),
   ('NAK_ARDRA','en','Latn',N'Ardra',NULL,N'Nakshatra 6 of 27.'),
   ('NAK_PUNARVASU','sa','Latn',N'Punarvasu',N'Punarvasu',NULL),
   ('NAK_PUNARVASU','en','Latn',N'Punarvasu',NULL,N'Nakshatra 7 of 27.'),
   ('NAK_PUSHYA','sa','Latn',N'Pushya',N'Pushya',NULL),
   ('NAK_PUSHYA','en','Latn',N'Pushya',NULL,N'Nakshatra 8 of 27.'),
   ('NAK_ASHLESHA','sa','Latn',N'Ashlesha',N'Ashlesha',NULL),
   ('NAK_ASHLESHA','en','Latn',N'Ashlesha',NULL,N'Nakshatra 9 of 27.'),
   ('NAK_MAGHA','sa','Latn',N'Magha',N'Magha',NULL),
   ('NAK_MAGHA','en','Latn',N'Magha',NULL,N'Nakshatra 10 of 27.'),
   ('NAK_PURVA_PHALGUNI','sa','Latn',N'Purva Phalguni',N'Purva Phalguni',NULL),
   ('NAK_PURVA_PHALGUNI','en','Latn',N'Purva Phalguni',NULL,N'Nakshatra 11 of 27.'),
   ('NAK_UTTARA_PHALGUNI','sa','Latn',N'Uttara Phalguni',N'Uttara Phalguni',NULL),
   ('NAK_UTTARA_PHALGUNI','en','Latn',N'Uttara Phalguni',NULL,N'Nakshatra 12 of 27.'),
   ('NAK_HASTA','sa','Latn',N'Hasta',N'Hasta',NULL),
   ('NAK_HASTA','en','Latn',N'Hasta',NULL,N'Nakshatra 13 of 27.'),
   ('NAK_CHITRA','sa','Latn',N'Chitra',N'Chitra',NULL),
   ('NAK_CHITRA','en','Latn',N'Chitra',NULL,N'Nakshatra 14 of 27.'),
   ('NAK_SWATI','sa','Latn',N'Swati',N'Swati',NULL),
   ('NAK_SWATI','en','Latn',N'Swati',NULL,N'Nakshatra 15 of 27.'),
   ('NAK_VISHAKHA','sa','Latn',N'Vishakha',N'Vishakha',NULL),
   ('NAK_VISHAKHA','en','Latn',N'Vishakha',NULL,N'Nakshatra 16 of 27.'),
   ('NAK_ANURADHA','sa','Latn',N'Anuradha',N'Anuradha',NULL),
   ('NAK_ANURADHA','en','Latn',N'Anuradha',NULL,N'Nakshatra 17 of 27.'),
   ('NAK_JYESHTHA','sa','Latn',N'Jyeshtha',N'Jyeshtha',NULL),
   ('NAK_JYESHTHA','en','Latn',N'Jyeshtha',NULL,N'Nakshatra 18 of 27.'),
   ('NAK_MULA','sa','Latn',N'Mula',N'Mula',NULL),
   ('NAK_MULA','en','Latn',N'Mula',NULL,N'Nakshatra 19 of 27.'),
   ('NAK_PURVA_ASHADHA','sa','Latn',N'Purva Ashadha',N'Purva Ashadha',NULL),
   ('NAK_PURVA_ASHADHA','en','Latn',N'Purva Ashadha',NULL,N'Nakshatra 20 of 27.'),
   ('NAK_UTTARA_ASHADHA','sa','Latn',N'Uttara Ashadha',N'Uttara Ashadha',NULL),
   ('NAK_UTTARA_ASHADHA','en','Latn',N'Uttara Ashadha',NULL,N'Nakshatra 21 of 27.'),
   ('NAK_SHRAVANA','sa','Latn',N'Shravana',N'Shravana',NULL),
   ('NAK_SHRAVANA','en','Latn',N'Shravana',NULL,N'Nakshatra 22 of 27.'),
   ('NAK_DHANISHTA','sa','Latn',N'Dhanishta',N'Dhanishta',NULL),
   ('NAK_DHANISHTA','en','Latn',N'Dhanishta',NULL,N'Nakshatra 23 of 27.'),
   ('NAK_SHATABHISHA','sa','Latn',N'Shatabhisha',N'Shatabhisha',NULL),
   ('NAK_SHATABHISHA','en','Latn',N'Shatabhisha',NULL,N'Nakshatra 24 of 27.'),
   ('NAK_PURVA_BHADRAPADA','sa','Latn',N'Purva Bhadrapada',N'Purva Bhadrapada',NULL),
   ('NAK_PURVA_BHADRAPADA','en','Latn',N'Purva Bhadrapada',NULL,N'Nakshatra 25 of 27.'),
   ('NAK_UTTARA_BHADRAPADA','sa','Latn',N'Uttara Bhadrapada',N'Uttara Bhadrapada',NULL),
   ('NAK_UTTARA_BHADRAPADA','en','Latn',N'Uttara Bhadrapada',NULL,N'Nakshatra 26 of 27.'),
   ('NAK_REVATI','sa','Latn',N'Revati',N'Revati',NULL),
   ('NAK_REVATI','en','Latn',N'Revati',NULL,N'Nakshatra 27 of 27.'),
   ('NAKPADA_ASHWINI_1','sa','Latn',N'Ashwini pada 1',N'Ashwini pada 1',NULL),
   ('NAKPADA_ASHWINI_1','en','Latn',N'Ashwini pada 1',NULL,N'Quarter 1 of nakshatra Ashwini.'),
   ('NAKPADA_ASHWINI_2','sa','Latn',N'Ashwini pada 2',N'Ashwini pada 2',NULL),
   ('NAKPADA_ASHWINI_2','en','Latn',N'Ashwini pada 2',NULL,N'Quarter 2 of nakshatra Ashwini.'),
   ('NAKPADA_ASHWINI_3','sa','Latn',N'Ashwini pada 3',N'Ashwini pada 3',NULL),
   ('NAKPADA_ASHWINI_3','en','Latn',N'Ashwini pada 3',NULL,N'Quarter 3 of nakshatra Ashwini.'),
   ('NAKPADA_ASHWINI_4','sa','Latn',N'Ashwini pada 4',N'Ashwini pada 4',NULL),
   ('NAKPADA_ASHWINI_4','en','Latn',N'Ashwini pada 4',NULL,N'Quarter 4 of nakshatra Ashwini.'),
   ('NAKPADA_BHARANI_1','sa','Latn',N'Bharani pada 1',N'Bharani pada 1',NULL),
   ('NAKPADA_BHARANI_1','en','Latn',N'Bharani pada 1',NULL,N'Quarter 1 of nakshatra Bharani.'),
   ('NAKPADA_BHARANI_2','sa','Latn',N'Bharani pada 2',N'Bharani pada 2',NULL),
   ('NAKPADA_BHARANI_2','en','Latn',N'Bharani pada 2',NULL,N'Quarter 2 of nakshatra Bharani.'),
   ('NAKPADA_BHARANI_3','sa','Latn',N'Bharani pada 3',N'Bharani pada 3',NULL),
   ('NAKPADA_BHARANI_3','en','Latn',N'Bharani pada 3',NULL,N'Quarter 3 of nakshatra Bharani.'),
   ('NAKPADA_BHARANI_4','sa','Latn',N'Bharani pada 4',N'Bharani pada 4',NULL),
   ('NAKPADA_BHARANI_4','en','Latn',N'Bharani pada 4',NULL,N'Quarter 4 of nakshatra Bharani.'),
   ('NAKPADA_KRITTIKA_1','sa','Latn',N'Krittika pada 1',N'Krittika pada 1',NULL),
   ('NAKPADA_KRITTIKA_1','en','Latn',N'Krittika pada 1',NULL,N'Quarter 1 of nakshatra Krittika.'),
   ('NAKPADA_KRITTIKA_2','sa','Latn',N'Krittika pada 2',N'Krittika pada 2',NULL),
   ('NAKPADA_KRITTIKA_2','en','Latn',N'Krittika pada 2',NULL,N'Quarter 2 of nakshatra Krittika.'),
   ('NAKPADA_KRITTIKA_3','sa','Latn',N'Krittika pada 3',N'Krittika pada 3',NULL),
   ('NAKPADA_KRITTIKA_3','en','Latn',N'Krittika pada 3',NULL,N'Quarter 3 of nakshatra Krittika.'),
   ('NAKPADA_KRITTIKA_4','sa','Latn',N'Krittika pada 4',N'Krittika pada 4',NULL),
   ('NAKPADA_KRITTIKA_4','en','Latn',N'Krittika pada 4',NULL,N'Quarter 4 of nakshatra Krittika.'),
   ('NAKPADA_ROHINI_1','sa','Latn',N'Rohini pada 1',N'Rohini pada 1',NULL),
   ('NAKPADA_ROHINI_1','en','Latn',N'Rohini pada 1',NULL,N'Quarter 1 of nakshatra Rohini.'),
   ('NAKPADA_ROHINI_2','sa','Latn',N'Rohini pada 2',N'Rohini pada 2',NULL),
   ('NAKPADA_ROHINI_2','en','Latn',N'Rohini pada 2',NULL,N'Quarter 2 of nakshatra Rohini.'),
   ('NAKPADA_ROHINI_3','sa','Latn',N'Rohini pada 3',N'Rohini pada 3',NULL),
   ('NAKPADA_ROHINI_3','en','Latn',N'Rohini pada 3',NULL,N'Quarter 3 of nakshatra Rohini.'),
   ('NAKPADA_ROHINI_4','sa','Latn',N'Rohini pada 4',N'Rohini pada 4',NULL),
   ('NAKPADA_ROHINI_4','en','Latn',N'Rohini pada 4',NULL,N'Quarter 4 of nakshatra Rohini.'),
   ('NAKPADA_MRIGASHIRA_1','sa','Latn',N'Mrigashira pada 1',N'Mrigashira pada 1',NULL),
   ('NAKPADA_MRIGASHIRA_1','en','Latn',N'Mrigashira pada 1',NULL,N'Quarter 1 of nakshatra Mrigashira.'),
   ('NAKPADA_MRIGASHIRA_2','sa','Latn',N'Mrigashira pada 2',N'Mrigashira pada 2',NULL),
   ('NAKPADA_MRIGASHIRA_2','en','Latn',N'Mrigashira pada 2',NULL,N'Quarter 2 of nakshatra Mrigashira.'),
   ('NAKPADA_MRIGASHIRA_3','sa','Latn',N'Mrigashira pada 3',N'Mrigashira pada 3',NULL),
   ('NAKPADA_MRIGASHIRA_3','en','Latn',N'Mrigashira pada 3',NULL,N'Quarter 3 of nakshatra Mrigashira.'),
   ('NAKPADA_MRIGASHIRA_4','sa','Latn',N'Mrigashira pada 4',N'Mrigashira pada 4',NULL),
   ('NAKPADA_MRIGASHIRA_4','en','Latn',N'Mrigashira pada 4',NULL,N'Quarter 4 of nakshatra Mrigashira.'),
   ('NAKPADA_ARDRA_1','sa','Latn',N'Ardra pada 1',N'Ardra pada 1',NULL),
   ('NAKPADA_ARDRA_1','en','Latn',N'Ardra pada 1',NULL,N'Quarter 1 of nakshatra Ardra.'),
   ('NAKPADA_ARDRA_2','sa','Latn',N'Ardra pada 2',N'Ardra pada 2',NULL),
   ('NAKPADA_ARDRA_2','en','Latn',N'Ardra pada 2',NULL,N'Quarter 2 of nakshatra Ardra.'),
   ('NAKPADA_ARDRA_3','sa','Latn',N'Ardra pada 3',N'Ardra pada 3',NULL),
   ('NAKPADA_ARDRA_3','en','Latn',N'Ardra pada 3',NULL,N'Quarter 3 of nakshatra Ardra.'),
   ('NAKPADA_ARDRA_4','sa','Latn',N'Ardra pada 4',N'Ardra pada 4',NULL),
   ('NAKPADA_ARDRA_4','en','Latn',N'Ardra pada 4',NULL,N'Quarter 4 of nakshatra Ardra.'),
   ('NAKPADA_PUNARVASU_1','sa','Latn',N'Punarvasu pada 1',N'Punarvasu pada 1',NULL),
   ('NAKPADA_PUNARVASU_1','en','Latn',N'Punarvasu pada 1',NULL,N'Quarter 1 of nakshatra Punarvasu.'),
   ('NAKPADA_PUNARVASU_2','sa','Latn',N'Punarvasu pada 2',N'Punarvasu pada 2',NULL),
   ('NAKPADA_PUNARVASU_2','en','Latn',N'Punarvasu pada 2',NULL,N'Quarter 2 of nakshatra Punarvasu.'),
   ('NAKPADA_PUNARVASU_3','sa','Latn',N'Punarvasu pada 3',N'Punarvasu pada 3',NULL),
   ('NAKPADA_PUNARVASU_3','en','Latn',N'Punarvasu pada 3',NULL,N'Quarter 3 of nakshatra Punarvasu.'),
   ('NAKPADA_PUNARVASU_4','sa','Latn',N'Punarvasu pada 4',N'Punarvasu pada 4',NULL),
   ('NAKPADA_PUNARVASU_4','en','Latn',N'Punarvasu pada 4',NULL,N'Quarter 4 of nakshatra Punarvasu.'),
   ('NAKPADA_PUSHYA_1','sa','Latn',N'Pushya pada 1',N'Pushya pada 1',NULL),
   ('NAKPADA_PUSHYA_1','en','Latn',N'Pushya pada 1',NULL,N'Quarter 1 of nakshatra Pushya.'),
   ('NAKPADA_PUSHYA_2','sa','Latn',N'Pushya pada 2',N'Pushya pada 2',NULL),
   ('NAKPADA_PUSHYA_2','en','Latn',N'Pushya pada 2',NULL,N'Quarter 2 of nakshatra Pushya.'),
   ('NAKPADA_PUSHYA_3','sa','Latn',N'Pushya pada 3',N'Pushya pada 3',NULL),
   ('NAKPADA_PUSHYA_3','en','Latn',N'Pushya pada 3',NULL,N'Quarter 3 of nakshatra Pushya.'),
   ('NAKPADA_PUSHYA_4','sa','Latn',N'Pushya pada 4',N'Pushya pada 4',NULL),
   ('NAKPADA_PUSHYA_4','en','Latn',N'Pushya pada 4',NULL,N'Quarter 4 of nakshatra Pushya.'),
   ('NAKPADA_ASHLESHA_1','sa','Latn',N'Ashlesha pada 1',N'Ashlesha pada 1',NULL),
   ('NAKPADA_ASHLESHA_1','en','Latn',N'Ashlesha pada 1',NULL,N'Quarter 1 of nakshatra Ashlesha.'),
   ('NAKPADA_ASHLESHA_2','sa','Latn',N'Ashlesha pada 2',N'Ashlesha pada 2',NULL),
   ('NAKPADA_ASHLESHA_2','en','Latn',N'Ashlesha pada 2',NULL,N'Quarter 2 of nakshatra Ashlesha.'),
   ('NAKPADA_ASHLESHA_3','sa','Latn',N'Ashlesha pada 3',N'Ashlesha pada 3',NULL),
   ('NAKPADA_ASHLESHA_3','en','Latn',N'Ashlesha pada 3',NULL,N'Quarter 3 of nakshatra Ashlesha.'),
   ('NAKPADA_ASHLESHA_4','sa','Latn',N'Ashlesha pada 4',N'Ashlesha pada 4',NULL),
   ('NAKPADA_ASHLESHA_4','en','Latn',N'Ashlesha pada 4',NULL,N'Quarter 4 of nakshatra Ashlesha.'),
   ('NAKPADA_MAGHA_1','sa','Latn',N'Magha pada 1',N'Magha pada 1',NULL),
   ('NAKPADA_MAGHA_1','en','Latn',N'Magha pada 1',NULL,N'Quarter 1 of nakshatra Magha.'),
   ('NAKPADA_MAGHA_2','sa','Latn',N'Magha pada 2',N'Magha pada 2',NULL),
   ('NAKPADA_MAGHA_2','en','Latn',N'Magha pada 2',NULL,N'Quarter 2 of nakshatra Magha.'),
   ('NAKPADA_MAGHA_3','sa','Latn',N'Magha pada 3',N'Magha pada 3',NULL),
   ('NAKPADA_MAGHA_3','en','Latn',N'Magha pada 3',NULL,N'Quarter 3 of nakshatra Magha.'),
   ('NAKPADA_MAGHA_4','sa','Latn',N'Magha pada 4',N'Magha pada 4',NULL),
   ('NAKPADA_MAGHA_4','en','Latn',N'Magha pada 4',NULL,N'Quarter 4 of nakshatra Magha.'),
   ('NAKPADA_PURVA_PHALGUNI_1','sa','Latn',N'Purva Phalguni pada 1',N'Purva Phalguni pada 1',NULL),
   ('NAKPADA_PURVA_PHALGUNI_1','en','Latn',N'Purva Phalguni pada 1',NULL,N'Quarter 1 of nakshatra Purva Phalguni.'),
   ('NAKPADA_PURVA_PHALGUNI_2','sa','Latn',N'Purva Phalguni pada 2',N'Purva Phalguni pada 2',NULL),
   ('NAKPADA_PURVA_PHALGUNI_2','en','Latn',N'Purva Phalguni pada 2',NULL,N'Quarter 2 of nakshatra Purva Phalguni.'),
   ('NAKPADA_PURVA_PHALGUNI_3','sa','Latn',N'Purva Phalguni pada 3',N'Purva Phalguni pada 3',NULL),
   ('NAKPADA_PURVA_PHALGUNI_3','en','Latn',N'Purva Phalguni pada 3',NULL,N'Quarter 3 of nakshatra Purva Phalguni.'),
   ('NAKPADA_PURVA_PHALGUNI_4','sa','Latn',N'Purva Phalguni pada 4',N'Purva Phalguni pada 4',NULL),
   ('NAKPADA_PURVA_PHALGUNI_4','en','Latn',N'Purva Phalguni pada 4',NULL,N'Quarter 4 of nakshatra Purva Phalguni.'),
   ('NAKPADA_UTTARA_PHALGUNI_1','sa','Latn',N'Uttara Phalguni pada 1',N'Uttara Phalguni pada 1',NULL),
   ('NAKPADA_UTTARA_PHALGUNI_1','en','Latn',N'Uttara Phalguni pada 1',NULL,N'Quarter 1 of nakshatra Uttara Phalguni.'),
   ('NAKPADA_UTTARA_PHALGUNI_2','sa','Latn',N'Uttara Phalguni pada 2',N'Uttara Phalguni pada 2',NULL),
   ('NAKPADA_UTTARA_PHALGUNI_2','en','Latn',N'Uttara Phalguni pada 2',NULL,N'Quarter 2 of nakshatra Uttara Phalguni.'),
   ('NAKPADA_UTTARA_PHALGUNI_3','sa','Latn',N'Uttara Phalguni pada 3',N'Uttara Phalguni pada 3',NULL),
   ('NAKPADA_UTTARA_PHALGUNI_3','en','Latn',N'Uttara Phalguni pada 3',NULL,N'Quarter 3 of nakshatra Uttara Phalguni.'),
   ('NAKPADA_UTTARA_PHALGUNI_4','sa','Latn',N'Uttara Phalguni pada 4',N'Uttara Phalguni pada 4',NULL),
   ('NAKPADA_UTTARA_PHALGUNI_4','en','Latn',N'Uttara Phalguni pada 4',NULL,N'Quarter 4 of nakshatra Uttara Phalguni.'),
   ('NAKPADA_HASTA_1','sa','Latn',N'Hasta pada 1',N'Hasta pada 1',NULL),
   ('NAKPADA_HASTA_1','en','Latn',N'Hasta pada 1',NULL,N'Quarter 1 of nakshatra Hasta.'),
   ('NAKPADA_HASTA_2','sa','Latn',N'Hasta pada 2',N'Hasta pada 2',NULL),
   ('NAKPADA_HASTA_2','en','Latn',N'Hasta pada 2',NULL,N'Quarter 2 of nakshatra Hasta.'),
   ('NAKPADA_HASTA_3','sa','Latn',N'Hasta pada 3',N'Hasta pada 3',NULL),
   ('NAKPADA_HASTA_3','en','Latn',N'Hasta pada 3',NULL,N'Quarter 3 of nakshatra Hasta.'),
   ('NAKPADA_HASTA_4','sa','Latn',N'Hasta pada 4',N'Hasta pada 4',NULL),
   ('NAKPADA_HASTA_4','en','Latn',N'Hasta pada 4',NULL,N'Quarter 4 of nakshatra Hasta.'),
   ('NAKPADA_CHITRA_1','sa','Latn',N'Chitra pada 1',N'Chitra pada 1',NULL),
   ('NAKPADA_CHITRA_1','en','Latn',N'Chitra pada 1',NULL,N'Quarter 1 of nakshatra Chitra.'),
   ('NAKPADA_CHITRA_2','sa','Latn',N'Chitra pada 2',N'Chitra pada 2',NULL),
   ('NAKPADA_CHITRA_2','en','Latn',N'Chitra pada 2',NULL,N'Quarter 2 of nakshatra Chitra.'),
   ('NAKPADA_CHITRA_3','sa','Latn',N'Chitra pada 3',N'Chitra pada 3',NULL),
   ('NAKPADA_CHITRA_3','en','Latn',N'Chitra pada 3',NULL,N'Quarter 3 of nakshatra Chitra.'),
   ('NAKPADA_CHITRA_4','sa','Latn',N'Chitra pada 4',N'Chitra pada 4',NULL),
   ('NAKPADA_CHITRA_4','en','Latn',N'Chitra pada 4',NULL,N'Quarter 4 of nakshatra Chitra.'),
   ('NAKPADA_SWATI_1','sa','Latn',N'Swati pada 1',N'Swati pada 1',NULL),
   ('NAKPADA_SWATI_1','en','Latn',N'Swati pada 1',NULL,N'Quarter 1 of nakshatra Swati.'),
   ('NAKPADA_SWATI_2','sa','Latn',N'Swati pada 2',N'Swati pada 2',NULL),
   ('NAKPADA_SWATI_2','en','Latn',N'Swati pada 2',NULL,N'Quarter 2 of nakshatra Swati.'),
   ('NAKPADA_SWATI_3','sa','Latn',N'Swati pada 3',N'Swati pada 3',NULL),
   ('NAKPADA_SWATI_3','en','Latn',N'Swati pada 3',NULL,N'Quarter 3 of nakshatra Swati.'),
   ('NAKPADA_SWATI_4','sa','Latn',N'Swati pada 4',N'Swati pada 4',NULL),
   ('NAKPADA_SWATI_4','en','Latn',N'Swati pada 4',NULL,N'Quarter 4 of nakshatra Swati.'),
   ('NAKPADA_VISHAKHA_1','sa','Latn',N'Vishakha pada 1',N'Vishakha pada 1',NULL),
   ('NAKPADA_VISHAKHA_1','en','Latn',N'Vishakha pada 1',NULL,N'Quarter 1 of nakshatra Vishakha.'),
   ('NAKPADA_VISHAKHA_2','sa','Latn',N'Vishakha pada 2',N'Vishakha pada 2',NULL),
   ('NAKPADA_VISHAKHA_2','en','Latn',N'Vishakha pada 2',NULL,N'Quarter 2 of nakshatra Vishakha.'),
   ('NAKPADA_VISHAKHA_3','sa','Latn',N'Vishakha pada 3',N'Vishakha pada 3',NULL),
   ('NAKPADA_VISHAKHA_3','en','Latn',N'Vishakha pada 3',NULL,N'Quarter 3 of nakshatra Vishakha.'),
   ('NAKPADA_VISHAKHA_4','sa','Latn',N'Vishakha pada 4',N'Vishakha pada 4',NULL),
   ('NAKPADA_VISHAKHA_4','en','Latn',N'Vishakha pada 4',NULL,N'Quarter 4 of nakshatra Vishakha.'),
   ('NAKPADA_ANURADHA_1','sa','Latn',N'Anuradha pada 1',N'Anuradha pada 1',NULL),
   ('NAKPADA_ANURADHA_1','en','Latn',N'Anuradha pada 1',NULL,N'Quarter 1 of nakshatra Anuradha.'),
   ('NAKPADA_ANURADHA_2','sa','Latn',N'Anuradha pada 2',N'Anuradha pada 2',NULL),
   ('NAKPADA_ANURADHA_2','en','Latn',N'Anuradha pada 2',NULL,N'Quarter 2 of nakshatra Anuradha.'),
   ('NAKPADA_ANURADHA_3','sa','Latn',N'Anuradha pada 3',N'Anuradha pada 3',NULL),
   ('NAKPADA_ANURADHA_3','en','Latn',N'Anuradha pada 3',NULL,N'Quarter 3 of nakshatra Anuradha.'),
   ('NAKPADA_ANURADHA_4','sa','Latn',N'Anuradha pada 4',N'Anuradha pada 4',NULL),
   ('NAKPADA_ANURADHA_4','en','Latn',N'Anuradha pada 4',NULL,N'Quarter 4 of nakshatra Anuradha.'),
   ('NAKPADA_JYESHTHA_1','sa','Latn',N'Jyeshtha pada 1',N'Jyeshtha pada 1',NULL),
   ('NAKPADA_JYESHTHA_1','en','Latn',N'Jyeshtha pada 1',NULL,N'Quarter 1 of nakshatra Jyeshtha.'),
   ('NAKPADA_JYESHTHA_2','sa','Latn',N'Jyeshtha pada 2',N'Jyeshtha pada 2',NULL),
   ('NAKPADA_JYESHTHA_2','en','Latn',N'Jyeshtha pada 2',NULL,N'Quarter 2 of nakshatra Jyeshtha.'),
   ('NAKPADA_JYESHTHA_3','sa','Latn',N'Jyeshtha pada 3',N'Jyeshtha pada 3',NULL),
   ('NAKPADA_JYESHTHA_3','en','Latn',N'Jyeshtha pada 3',NULL,N'Quarter 3 of nakshatra Jyeshtha.'),
   ('NAKPADA_JYESHTHA_4','sa','Latn',N'Jyeshtha pada 4',N'Jyeshtha pada 4',NULL),
   ('NAKPADA_JYESHTHA_4','en','Latn',N'Jyeshtha pada 4',NULL,N'Quarter 4 of nakshatra Jyeshtha.'),
   ('NAKPADA_MULA_1','sa','Latn',N'Mula pada 1',N'Mula pada 1',NULL),
   ('NAKPADA_MULA_1','en','Latn',N'Mula pada 1',NULL,N'Quarter 1 of nakshatra Mula.'),
   ('NAKPADA_MULA_2','sa','Latn',N'Mula pada 2',N'Mula pada 2',NULL),
   ('NAKPADA_MULA_2','en','Latn',N'Mula pada 2',NULL,N'Quarter 2 of nakshatra Mula.'),
   ('NAKPADA_MULA_3','sa','Latn',N'Mula pada 3',N'Mula pada 3',NULL),
   ('NAKPADA_MULA_3','en','Latn',N'Mula pada 3',NULL,N'Quarter 3 of nakshatra Mula.'),
   ('NAKPADA_MULA_4','sa','Latn',N'Mula pada 4',N'Mula pada 4',NULL),
   ('NAKPADA_MULA_4','en','Latn',N'Mula pada 4',NULL,N'Quarter 4 of nakshatra Mula.'),
   ('NAKPADA_PURVA_ASHADHA_1','sa','Latn',N'Purva Ashadha pada 1',N'Purva Ashadha pada 1',NULL),
   ('NAKPADA_PURVA_ASHADHA_1','en','Latn',N'Purva Ashadha pada 1',NULL,N'Quarter 1 of nakshatra Purva Ashadha.'),
   ('NAKPADA_PURVA_ASHADHA_2','sa','Latn',N'Purva Ashadha pada 2',N'Purva Ashadha pada 2',NULL),
   ('NAKPADA_PURVA_ASHADHA_2','en','Latn',N'Purva Ashadha pada 2',NULL,N'Quarter 2 of nakshatra Purva Ashadha.'),
   ('NAKPADA_PURVA_ASHADHA_3','sa','Latn',N'Purva Ashadha pada 3',N'Purva Ashadha pada 3',NULL),
   ('NAKPADA_PURVA_ASHADHA_3','en','Latn',N'Purva Ashadha pada 3',NULL,N'Quarter 3 of nakshatra Purva Ashadha.'),
   ('NAKPADA_PURVA_ASHADHA_4','sa','Latn',N'Purva Ashadha pada 4',N'Purva Ashadha pada 4',NULL),
   ('NAKPADA_PURVA_ASHADHA_4','en','Latn',N'Purva Ashadha pada 4',NULL,N'Quarter 4 of nakshatra Purva Ashadha.'),
   ('NAKPADA_UTTARA_ASHADHA_1','sa','Latn',N'Uttara Ashadha pada 1',N'Uttara Ashadha pada 1',NULL),
   ('NAKPADA_UTTARA_ASHADHA_1','en','Latn',N'Uttara Ashadha pada 1',NULL,N'Quarter 1 of nakshatra Uttara Ashadha.'),
   ('NAKPADA_UTTARA_ASHADHA_2','sa','Latn',N'Uttara Ashadha pada 2',N'Uttara Ashadha pada 2',NULL),
   ('NAKPADA_UTTARA_ASHADHA_2','en','Latn',N'Uttara Ashadha pada 2',NULL,N'Quarter 2 of nakshatra Uttara Ashadha.'),
   ('NAKPADA_UTTARA_ASHADHA_3','sa','Latn',N'Uttara Ashadha pada 3',N'Uttara Ashadha pada 3',NULL),
   ('NAKPADA_UTTARA_ASHADHA_3','en','Latn',N'Uttara Ashadha pada 3',NULL,N'Quarter 3 of nakshatra Uttara Ashadha.'),
   ('NAKPADA_UTTARA_ASHADHA_4','sa','Latn',N'Uttara Ashadha pada 4',N'Uttara Ashadha pada 4',NULL),
   ('NAKPADA_UTTARA_ASHADHA_4','en','Latn',N'Uttara Ashadha pada 4',NULL,N'Quarter 4 of nakshatra Uttara Ashadha.'),
   ('NAKPADA_SHRAVANA_1','sa','Latn',N'Shravana pada 1',N'Shravana pada 1',NULL),
   ('NAKPADA_SHRAVANA_1','en','Latn',N'Shravana pada 1',NULL,N'Quarter 1 of nakshatra Shravana.'),
   ('NAKPADA_SHRAVANA_2','sa','Latn',N'Shravana pada 2',N'Shravana pada 2',NULL),
   ('NAKPADA_SHRAVANA_2','en','Latn',N'Shravana pada 2',NULL,N'Quarter 2 of nakshatra Shravana.'),
   ('NAKPADA_SHRAVANA_3','sa','Latn',N'Shravana pada 3',N'Shravana pada 3',NULL),
   ('NAKPADA_SHRAVANA_3','en','Latn',N'Shravana pada 3',NULL,N'Quarter 3 of nakshatra Shravana.'),
   ('NAKPADA_SHRAVANA_4','sa','Latn',N'Shravana pada 4',N'Shravana pada 4',NULL),
   ('NAKPADA_SHRAVANA_4','en','Latn',N'Shravana pada 4',NULL,N'Quarter 4 of nakshatra Shravana.'),
   ('NAKPADA_DHANISHTA_1','sa','Latn',N'Dhanishta pada 1',N'Dhanishta pada 1',NULL),
   ('NAKPADA_DHANISHTA_1','en','Latn',N'Dhanishta pada 1',NULL,N'Quarter 1 of nakshatra Dhanishta.'),
   ('NAKPADA_DHANISHTA_2','sa','Latn',N'Dhanishta pada 2',N'Dhanishta pada 2',NULL),
   ('NAKPADA_DHANISHTA_2','en','Latn',N'Dhanishta pada 2',NULL,N'Quarter 2 of nakshatra Dhanishta.'),
   ('NAKPADA_DHANISHTA_3','sa','Latn',N'Dhanishta pada 3',N'Dhanishta pada 3',NULL),
   ('NAKPADA_DHANISHTA_3','en','Latn',N'Dhanishta pada 3',NULL,N'Quarter 3 of nakshatra Dhanishta.'),
   ('NAKPADA_DHANISHTA_4','sa','Latn',N'Dhanishta pada 4',N'Dhanishta pada 4',NULL),
   ('NAKPADA_DHANISHTA_4','en','Latn',N'Dhanishta pada 4',NULL,N'Quarter 4 of nakshatra Dhanishta.'),
   ('NAKPADA_SHATABHISHA_1','sa','Latn',N'Shatabhisha pada 1',N'Shatabhisha pada 1',NULL),
   ('NAKPADA_SHATABHISHA_1','en','Latn',N'Shatabhisha pada 1',NULL,N'Quarter 1 of nakshatra Shatabhisha.'),
   ('NAKPADA_SHATABHISHA_2','sa','Latn',N'Shatabhisha pada 2',N'Shatabhisha pada 2',NULL),
   ('NAKPADA_SHATABHISHA_2','en','Latn',N'Shatabhisha pada 2',NULL,N'Quarter 2 of nakshatra Shatabhisha.'),
   ('NAKPADA_SHATABHISHA_3','sa','Latn',N'Shatabhisha pada 3',N'Shatabhisha pada 3',NULL),
   ('NAKPADA_SHATABHISHA_3','en','Latn',N'Shatabhisha pada 3',NULL,N'Quarter 3 of nakshatra Shatabhisha.'),
   ('NAKPADA_SHATABHISHA_4','sa','Latn',N'Shatabhisha pada 4',N'Shatabhisha pada 4',NULL),
   ('NAKPADA_SHATABHISHA_4','en','Latn',N'Shatabhisha pada 4',NULL,N'Quarter 4 of nakshatra Shatabhisha.'),
   ('NAKPADA_PURVA_BHADRAPADA_1','sa','Latn',N'Purva Bhadrapada pada 1',N'Purva Bhadrapada pada 1',NULL),
   ('NAKPADA_PURVA_BHADRAPADA_1','en','Latn',N'Purva Bhadrapada pada 1',NULL,N'Quarter 1 of nakshatra Purva Bhadrapada.'),
   ('NAKPADA_PURVA_BHADRAPADA_2','sa','Latn',N'Purva Bhadrapada pada 2',N'Purva Bhadrapada pada 2',NULL),
   ('NAKPADA_PURVA_BHADRAPADA_2','en','Latn',N'Purva Bhadrapada pada 2',NULL,N'Quarter 2 of nakshatra Purva Bhadrapada.'),
   ('NAKPADA_PURVA_BHADRAPADA_3','sa','Latn',N'Purva Bhadrapada pada 3',N'Purva Bhadrapada pada 3',NULL),
   ('NAKPADA_PURVA_BHADRAPADA_3','en','Latn',N'Purva Bhadrapada pada 3',NULL,N'Quarter 3 of nakshatra Purva Bhadrapada.'),
   ('NAKPADA_PURVA_BHADRAPADA_4','sa','Latn',N'Purva Bhadrapada pada 4',N'Purva Bhadrapada pada 4',NULL),
   ('NAKPADA_PURVA_BHADRAPADA_4','en','Latn',N'Purva Bhadrapada pada 4',NULL,N'Quarter 4 of nakshatra Purva Bhadrapada.'),
   ('NAKPADA_UTTARA_BHADRAPADA_1','sa','Latn',N'Uttara Bhadrapada pada 1',N'Uttara Bhadrapada pada 1',NULL),
   ('NAKPADA_UTTARA_BHADRAPADA_1','en','Latn',N'Uttara Bhadrapada pada 1',NULL,N'Quarter 1 of nakshatra Uttara Bhadrapada.'),
   ('NAKPADA_UTTARA_BHADRAPADA_2','sa','Latn',N'Uttara Bhadrapada pada 2',N'Uttara Bhadrapada pada 2',NULL),
   ('NAKPADA_UTTARA_BHADRAPADA_2','en','Latn',N'Uttara Bhadrapada pada 2',NULL,N'Quarter 2 of nakshatra Uttara Bhadrapada.'),
   ('NAKPADA_UTTARA_BHADRAPADA_3','sa','Latn',N'Uttara Bhadrapada pada 3',N'Uttara Bhadrapada pada 3',NULL),
   ('NAKPADA_UTTARA_BHADRAPADA_3','en','Latn',N'Uttara Bhadrapada pada 3',NULL,N'Quarter 3 of nakshatra Uttara Bhadrapada.'),
   ('NAKPADA_UTTARA_BHADRAPADA_4','sa','Latn',N'Uttara Bhadrapada pada 4',N'Uttara Bhadrapada pada 4',NULL),
   ('NAKPADA_UTTARA_BHADRAPADA_4','en','Latn',N'Uttara Bhadrapada pada 4',NULL,N'Quarter 4 of nakshatra Uttara Bhadrapada.'),
   ('NAKPADA_REVATI_1','sa','Latn',N'Revati pada 1',N'Revati pada 1',NULL),
   ('NAKPADA_REVATI_1','en','Latn',N'Revati pada 1',NULL,N'Quarter 1 of nakshatra Revati.'),
   ('NAKPADA_REVATI_2','sa','Latn',N'Revati pada 2',N'Revati pada 2',NULL),
   ('NAKPADA_REVATI_2','en','Latn',N'Revati pada 2',NULL,N'Quarter 2 of nakshatra Revati.'),
   ('NAKPADA_REVATI_3','sa','Latn',N'Revati pada 3',N'Revati pada 3',NULL),
   ('NAKPADA_REVATI_3','en','Latn',N'Revati pada 3',NULL,N'Quarter 3 of nakshatra Revati.'),
   ('NAKPADA_REVATI_4','sa','Latn',N'Revati pada 4',N'Revati pada 4',NULL),
   ('NAKPADA_REVATI_4','en','Latn',N'Revati pada 4',NULL,N'Quarter 4 of nakshatra Revati.'),
   ('VARGA_D1','sa','Latn',N'Rasi',N'Rasi',NULL),
   ('VARGA_D1','en','Latn',N'Rasi chart (D1)',NULL,N'Divisional chart D1: 1-part division of each rasi.'),
   ('VARGA_D2','sa','Latn',N'Hora',N'Hora',NULL),
   ('VARGA_D2','en','Latn',N'Hora chart (D2)',NULL,N'Divisional chart D2: 2-part division of each rasi.'),
   ('VARGA_D6','sa','Latn',N'Shashtamsa',N'Shashtamsa',NULL),
   ('VARGA_D6','en','Latn',N'Shashtamsa chart (D6)',NULL,N'Divisional chart D6: 6-part division of each rasi.'),
   ('VARGA_D9','sa','Latn',N'Navamsa',N'Navamsa',NULL),
   ('VARGA_D9','en','Latn',N'Navamsa chart (D9)',NULL,N'Divisional chart D9: 9-part division of each rasi.'),
   ('VARGA_D10','sa','Latn',N'Dasamsa',N'Dasamsa',NULL),
   ('VARGA_D10','en','Latn',N'Dasamsa chart (D10)',NULL,N'Divisional chart D10: 10-part division of each rasi.'),
   ('VARGA_D11','sa','Latn',N'Rudramsa',N'Rudramsa',NULL),
   ('VARGA_D11','en','Latn',N'Rudramsa chart (D11)',NULL,N'Divisional chart D11: 11-part division of each rasi.'),
   ('VARGA_D2_US','sa','Latn',N'Hora (Uma Shambu)',N'Hora (Uma Shambu)',NULL),
   ('VARGA_D2_US','en','Latn',N'Hora (Uma Shambu) chart (D2-US)',NULL,N'Divisional chart D2-US: 2-part division of each rasi.'),
   ('VARGA_D3','sa','Latn',N'Drekkana',N'Drekkana',NULL),
   ('VARGA_D3','en','Latn',N'Drekkana chart (D3)',NULL,N'Divisional chart D3: 3-part division of each rasi.'),
   ('VARGA_D4','sa','Latn',N'Chaturthamsa',N'Chaturthamsa',NULL),
   ('VARGA_D4','en','Latn',N'Chaturthamsa chart (D4)',NULL,N'Divisional chart D4: 4-part division of each rasi.'),
   ('VARGA_D5','sa','Latn',N'Panchamsa',N'Panchamsa',NULL),
   ('VARGA_D5','en','Latn',N'Panchamsa chart (D5)',NULL,N'Divisional chart D5: 5-part division of each rasi.'),
   ('VARGA_D7','sa','Latn',N'Saptamsa',N'Saptamsa',NULL),
   ('VARGA_D7','en','Latn',N'Saptamsa chart (D7)',NULL,N'Divisional chart D7: 7-part division of each rasi.'),
   ('VARGA_D8','sa','Latn',N'Ashtamsa',N'Ashtamsa',NULL),
   ('VARGA_D8','en','Latn',N'Ashtamsa chart (D8)',NULL,N'Divisional chart D8: 8-part division of each rasi.'),
   ('VARGA_D12','sa','Latn',N'Dwadasamsa',N'Dwadasamsa',NULL),
   ('VARGA_D12','en','Latn',N'Dwadasamsa chart (D12)',NULL,N'Divisional chart D12: 12-part division of each rasi.'),
   ('VARGA_D16','sa','Latn',N'Shodasamsa',N'Shodasamsa',NULL),
   ('VARGA_D16','en','Latn',N'Shodasamsa chart (D16)',NULL,N'Divisional chart D16: 16-part division of each rasi.'),
   ('VARGA_D20','sa','Latn',N'Vimsamsa',N'Vimsamsa',NULL),
   ('VARGA_D20','en','Latn',N'Vimsamsa chart (D20)',NULL,N'Divisional chart D20: 20-part division of each rasi.'),
   ('VARGA_D24','sa','Latn',N'Siddhamsa',N'Siddhamsa',NULL),
   ('VARGA_D24','en','Latn',N'Siddhamsa chart (D24)',NULL,N'Divisional chart D24: 24-part division of each rasi.'),
   ('VARGA_D27','sa','Latn',N'Nakshatramsa',N'Nakshatramsa',NULL),
   ('VARGA_D27','en','Latn',N'Nakshatramsa chart (D27)',NULL,N'Divisional chart D27: 27-part division of each rasi.'),
   ('VARGA_D30','sa','Latn',N'Trimsamsa',N'Trimsamsa',NULL),
   ('VARGA_D30','en','Latn',N'Trimsamsa chart (D30)',NULL,N'Divisional chart D30: 30-part division of each rasi.'),
   ('VARGA_D40','sa','Latn',N'Khavedamsa',N'Khavedamsa',NULL),
   ('VARGA_D40','en','Latn',N'Khavedamsa chart (D40)',NULL,N'Divisional chart D40: 40-part division of each rasi.'),
   ('VARGA_D45','sa','Latn',N'Akshavedamsa',N'Akshavedamsa',NULL),
   ('VARGA_D45','en','Latn',N'Akshavedamsa chart (D45)',NULL,N'Divisional chart D45: 45-part division of each rasi.'),
   ('VARGA_D60','sa','Latn',N'Shashtyamsa',N'Shashtyamsa',NULL),
   ('VARGA_D60','en','Latn',N'Shashtyamsa chart (D60)',NULL,N'Divisional chart D60: 60-part division of each rasi.'),
   ('KARAKA_AK','sa','Latn',N'Atmakaraka',N'Atmakaraka',NULL),
   ('KARAKA_AK','en','Latn',N'Self / soul significator',NULL,N'Jaimini chara karaka, rank 1 of 8 by descending longitude.'),
   ('KARAKA_AMK','sa','Latn',N'Amatyakaraka',N'Amatyakaraka',NULL),
   ('KARAKA_AMK','en','Latn',N'Career / minister significator',NULL,N'Jaimini chara karaka, rank 2 of 8 by descending longitude.'),
   ('KARAKA_BK','sa','Latn',N'Bhratrikaraka',N'Bhratrikaraka',NULL),
   ('KARAKA_BK','en','Latn',N'Siblings significator',NULL,N'Jaimini chara karaka, rank 3 of 8 by descending longitude.'),
   ('KARAKA_MK','sa','Latn',N'Matrikaraka',N'Matrikaraka',NULL),
   ('KARAKA_MK','en','Latn',N'Mother significator',NULL,N'Jaimini chara karaka, rank 4 of 8 by descending longitude.'),
   ('KARAKA_PIK','sa','Latn',N'Pitrikaraka',N'Pitrikaraka',NULL),
   ('KARAKA_PIK','en','Latn',N'Father significator',NULL,N'Jaimini chara karaka, rank 5 of 8 by descending longitude.'),
   ('KARAKA_PK','sa','Latn',N'Putrakaraka',N'Putrakaraka',NULL),
   ('KARAKA_PK','en','Latn',N'Children significator',NULL,N'Jaimini chara karaka, rank 6 of 8 by descending longitude.'),
   ('KARAKA_GK','sa','Latn',N'Gnatikaraka',N'Gnatikaraka',NULL),
   ('KARAKA_GK','en','Latn',N'Kin / rivalry significator',NULL,N'Jaimini chara karaka, rank 7 of 8 by descending longitude.'),
   ('KARAKA_DK','sa','Latn',N'Darakaraka',N'Darakaraka',NULL),
   ('KARAKA_DK','en','Latn',N'Spouse significator',NULL,N'Jaimini chara karaka, rank 8 of 8 by descending longitude.'),
   ('SPT_AL','sa','Latn',N'Arudha Lagna',N'Arudha Lagna',NULL),
   ('SPT_AL','en','Latn',N'Arudha Lagna',NULL,N'Sign-image of the ascendant (Jaimini pada of the 1st house).'),
   ('SPT_A2','sa','Latn',N'Bhava Arudha 2',N'Bhava Arudha 2',NULL),
   ('SPT_A2','en','Latn',N'Arudha of house 2',NULL,N'Jaimini pada (arudha) of house 2.'),
   ('SPT_A3','sa','Latn',N'Bhava Arudha 3',N'Bhava Arudha 3',NULL),
   ('SPT_A3','en','Latn',N'Arudha of house 3',NULL,N'Jaimini pada (arudha) of house 3.'),
   ('SPT_A4','sa','Latn',N'Bhava Arudha 4',N'Bhava Arudha 4',NULL),
   ('SPT_A4','en','Latn',N'Arudha of house 4',NULL,N'Jaimini pada (arudha) of house 4.'),
   ('SPT_A5','sa','Latn',N'Bhava Arudha 5',N'Bhava Arudha 5',NULL),
   ('SPT_A5','en','Latn',N'Arudha of house 5',NULL,N'Jaimini pada (arudha) of house 5.'),
   ('SPT_A6','sa','Latn',N'Bhava Arudha 6',N'Bhava Arudha 6',NULL),
   ('SPT_A6','en','Latn',N'Arudha of house 6',NULL,N'Jaimini pada (arudha) of house 6.'),
   ('SPT_A7','sa','Latn',N'Bhava Arudha 7',N'Bhava Arudha 7',NULL),
   ('SPT_A7','en','Latn',N'Arudha of house 7',NULL,N'Jaimini pada (arudha) of house 7.'),
   ('SPT_A8','sa','Latn',N'Bhava Arudha 8',N'Bhava Arudha 8',NULL),
   ('SPT_A8','en','Latn',N'Arudha of house 8',NULL,N'Jaimini pada (arudha) of house 8.'),
   ('SPT_A9','sa','Latn',N'Bhava Arudha 9',N'Bhava Arudha 9',NULL),
   ('SPT_A9','en','Latn',N'Arudha of house 9',NULL,N'Jaimini pada (arudha) of house 9.'),
   ('SPT_A10','sa','Latn',N'Bhava Arudha 10',N'Bhava Arudha 10',NULL),
   ('SPT_A10','en','Latn',N'Arudha of house 10',NULL,N'Jaimini pada (arudha) of house 10.'),
   ('SPT_A11','sa','Latn',N'Bhava Arudha 11',N'Bhava Arudha 11',NULL),
   ('SPT_A11','en','Latn',N'Arudha of house 11',NULL,N'Jaimini pada (arudha) of house 11.'),
   ('SPT_A12','sa','Latn',N'Bhava Arudha 12',N'Bhava Arudha 12',NULL),
   ('SPT_A12','en','Latn',N'Arudha of house 12',NULL,N'Jaimini pada (arudha) of house 12.'),
   ('SPT_HL','sa','Latn',N'Hora Lagna',N'Hora Lagna',NULL),
   ('SPT_HL','en','Latn',N'Hora Lagna',NULL,N'Time-based lagna advancing one sign per hora from sunrise.'),
   ('SPT_GULIKA','sa','Latn',N'Gulika',N'Gulika',NULL),
   ('SPT_GULIKA','en','Latn',N'Gulika',NULL,N'Upagraha marking the start of the Saturn sub-part of the day or night arc.'),
   ('SPT_MAANDI','sa','Latn',N'Maandi',N'Maandi',NULL),
   ('SPT_MAANDI','en','Latn',N'Maandi',NULL,N'Upagraha tied to the Saturn sub-division of the day or night arc.'),
   ('AVASTHA_BAALADI_BAALA','sa','Latn',N'Baala',N'Baala',NULL),
   ('AVASTHA_BAALADI_BAALA','en','Latn',N'Infant',NULL,N'Infant stage - one quarter of the normal effect.'),
   ('AVASTHA_BAALADI_KUMARA','sa','Latn',N'Kumara',N'Kumara',NULL),
   ('AVASTHA_BAALADI_KUMARA','en','Latn',N'Child',NULL,N'Child stage - one half of the normal effect.'),
   ('AVASTHA_BAALADI_YUVA','sa','Latn',N'Yuva',N'Yuva',NULL),
   ('AVASTHA_BAALADI_YUVA','en','Latn',N'Youth',NULL,N'Youth stage - full effect.'),
   ('AVASTHA_BAALADI_VRIDDHA','sa','Latn',N'Vriddha',N'Vriddha',NULL),
   ('AVASTHA_BAALADI_VRIDDHA','en','Latn',N'Old',NULL,N'Old stage - feeble effect.'),
   ('AVASTHA_BAALADI_MRITA','sa','Latn',N'Mrita',N'Mrita',NULL),
   ('AVASTHA_BAALADI_MRITA','en','Latn',N'Dead',NULL,N'Dead stage - no effect.'),
   ('AVASTHA_JAGRADADI_JAGRAT','sa','Latn',N'Jagrat',N'Jagrat',NULL),
   ('AVASTHA_JAGRADADI_JAGRAT','en','Latn',N'Awake',NULL,N'Awake - full result (own sign, exaltation or moolatrikona).'),
   ('AVASTHA_JAGRADADI_SWAPNA','sa','Latn',N'Swapna',N'Swapna',NULL),
   ('AVASTHA_JAGRADADI_SWAPNA','en','Latn',N'Dreaming',NULL,N'Dreaming - middling result (friendly or neutral sign).'),
   ('AVASTHA_JAGRADADI_SUSHUPTI','sa','Latn',N'Sushupti',N'Sushupti',NULL),
   ('AVASTHA_JAGRADADI_SUSHUPTI','en','Latn',N'Sleeping',NULL,N'Sleeping - weak result (enemy sign or debilitation).'),
   ('DIGNITY_EXALTED','sa','Latn',N'Uccha',N'Uccha',NULL),
   ('DIGNITY_EXALTED','en','Latn',N'Exalted',NULL,N'Planetary dignity: Exalted.'),
   ('DIGNITY_OWN_SIGN','sa','Latn',N'Svakshetra',N'Svakshetra',NULL),
   ('DIGNITY_OWN_SIGN','en','Latn',N'Own Sign',NULL,N'Planetary dignity: Own Sign.'),
   ('DIGNITY_MOOLATRIKONA','sa','Latn',N'Moolatrikona',N'Moolatrikona',NULL),
   ('DIGNITY_MOOLATRIKONA','en','Latn',N'Moolatrikona',NULL,N'Planetary dignity: Moolatrikona.'),
   ('DIGNITY_GREAT_FRIEND','sa','Latn',N'Adhimitra',N'Adhimitra',NULL),
   ('DIGNITY_GREAT_FRIEND','en','Latn',N'Great Friend',NULL,N'Planetary dignity: Great Friend.'),
   ('DIGNITY_FRIEND','sa','Latn',N'Mitra',N'Mitra',NULL),
   ('DIGNITY_FRIEND','en','Latn',N'Friend',NULL,N'Planetary dignity: Friend.'),
   ('DIGNITY_NEUTRAL','sa','Latn',N'Sama',N'Sama',NULL),
   ('DIGNITY_NEUTRAL','en','Latn',N'Neutral',NULL,N'Planetary dignity: Neutral.'),
   ('DIGNITY_ENEMY','sa','Latn',N'Shatru',N'Shatru',NULL),
   ('DIGNITY_ENEMY','en','Latn',N'Enemy',NULL,N'Planetary dignity: Enemy.'),
   ('DIGNITY_GREAT_ENEMY','sa','Latn',N'Adhishatru',N'Adhishatru',NULL),
   ('DIGNITY_GREAT_ENEMY','en','Latn',N'Great Enemy',NULL,N'Planetary dignity: Great Enemy.'),
   ('DIGNITY_DEBILITATED','sa','Latn',N'Neecha',N'Neecha',NULL),
   ('DIGNITY_DEBILITATED','en','Latn',N'Debilitated',NULL,N'Planetary dignity: Debilitated.'),
   ('REL_YUTI','sa','Latn',N'Yuti',N'Yuti',NULL),
   ('REL_YUTI','en','Latn',N'Conjunction',NULL,N'Two grahas occupying the same sign.'),
   ('REL_DRISHTI','sa','Latn',N'Drishti',N'Drishti',NULL),
   ('REL_DRISHTI','en','Latn',N'Aspect',NULL,N'A graha casting its glance onto another sign or graha.'),
   ('REL_COMBUST','sa','Latn',N'Asta',N'Asta',NULL),
   ('REL_COMBUST','en','Latn',N'Combustion',NULL,N'A graha too close to the Sun to give its results.'),
   ('REL_GREAT_FRIEND','sa','Latn',N'Adhimitra',N'Adhimitra',NULL),
   ('REL_GREAT_FRIEND','en','Latn',N'Great friend',NULL,N'Compound (natural plus temporal) friendship.'),
   ('REL_FRIEND','sa','Latn',N'Mitra',N'Mitra',NULL),
   ('REL_FRIEND','en','Latn',N'Friend',NULL,N'Natural friendship between two grahas.'),
   ('REL_ENEMY','sa','Latn',N'Shatru',N'Shatru',NULL),
   ('REL_ENEMY','en','Latn',N'Enemy',NULL,N'Natural enmity between two grahas.'),
   ('AYANAMSA_LAHIRI','sa','Latn',N'Lahiri',N'Lahiri',NULL),
   ('AYANAMSA_LAHIRI','en','Latn',N'Lahiri ayanamsa',NULL,N'Chitrapaksha sidereal zero-point; the default ayanamsa for this project.')
  ) AS v (Code, LanguageCode, Script, Name, TraditionalName, ShortDescription)
  JOIN dbo.tbl_Astro_Terminology t ON t.Code = v.Code
) AS src
ON tgt.TerminologyId = src.TerminologyId AND tgt.LanguageCode = src.LanguageCode AND tgt.Script = src.Script
WHEN MATCHED THEN UPDATE SET Name = src.Name, TraditionalName = src.TraditionalName, ShortDescription = src.ShortDescription
WHEN NOT MATCHED THEN INSERT (TerminologyId, LanguageCode, Script, Name, TraditionalName, ShortDescription)
    VALUES (src.TerminologyId, src.LanguageCode, src.Script, src.Name, src.TraditionalName, src.ShortDescription);
GO
-- <<< END TERMINOLOGY SEED <<<
-- tbl_ChartResults -> tbl_Rule_Sets / tbl_Dim_ChartType foreign keys
-- (folded from db/06_add_chartfact_constraints.sql). Declared here rather than inline in the
-- tbl_ChartResults CREATE TABLE because both referenced tables are created later in this script.
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ChartResults_RuleSet')
ALTER TABLE dbo.tbl_ChartResults ADD CONSTRAINT FK_ChartResults_RuleSet FOREIGN KEY (RuleSetId) REFERENCES dbo.tbl_Rule_Sets (Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ChartResults_ChartType')
ALTER TABLE dbo.tbl_ChartResults ADD CONSTRAINT FK_ChartResults_ChartType FOREIGN KEY (ChartTypeId) REFERENCES dbo.tbl_Dim_ChartType (Id);
GO

-- =====================================================================
-- tbl_Rule_VargaScheme — how each varga chart type derives a planet's
-- varga sign, per rule-set (folded from db/11_create_rule_vargascheme.sql).
-- Read by C# (VargaSignRuleFactory) and, later, by the Python comparison
-- layer. D1 is the identity rasi and is NOT here. SignRuleKey names the
-- C# IVargaSignRule; the l-part formulae are in the Plan-A spec 3.2
-- (traced to PyJHora horoscope/chart/charts.py). Placed after the
-- tbl_Dim_ChartType and tbl_Rule_Sets seeds because the seed JOINs the
-- former and FKs the latter. Idempotent.
-- =====================================================================
IF OBJECT_ID('dbo.tbl_Rule_VargaScheme', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_VargaScheme (
    Id             TINYINT      NOT NULL CONSTRAINT PK_Rule_VargaScheme PRIMARY KEY,
    RuleSetId      TINYINT      NOT NULL CONSTRAINT FK_Rule_VargaScheme_RuleSet   FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
    ChartTypeId    TINYINT      NOT NULL CONSTRAINT FK_Rule_VargaScheme_ChartType FOREIGN KEY REFERENCES dbo.tbl_Dim_ChartType (Id),
    DivisionFactor TINYINT      NOT NULL,
    MethodCode     VARCHAR(40)  NOT NULL,
    MethodSource   VARCHAR(200) NOT NULL,
    SignRuleKind   VARCHAR(10)  NOT NULL,
    SignRuleKey    VARCHAR(40)  NOT NULL,
    RuleParametersJson   NVARCHAR(MAX) NULL,
    CalculationNarrative NVARCHAR(MAX) NULL,
    SourceRefCode        VARCHAR(40)   NULL,
    IsActive             BIT NOT NULL CONSTRAINT DF_Rule_VargaScheme_IsActive DEFAULT 1,
    CONSTRAINT UQ_Rule_VargaScheme UNIQUE (RuleSetId, ChartTypeId),
    CONSTRAINT CK_RuleVarga_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
    CONSTRAINT CK_RuleVarga_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_VargaScheme)
INSERT dbo.tbl_Rule_VargaScheme
    (Id, RuleSetId, ChartTypeId, DivisionFactor, MethodCode, MethodSource, SignRuleKind, SignRuleKey)
SELECT v.Id, 1, ct.Id, v.N, v.MethodCode, v.MethodSource, v.Kind, v.SignRuleKey
FROM (VALUES
    ( 1, 'D2',    2,  'ClassicalTwoSign',    'BPHS two-sign Cn/Le; AstroMath.GetHoraSign',                       'Special', 'HoraD2Classic'),
    ( 2, 'D2-US', 2,  'UmaShambu',           'Parasara Uma Shambu; PyJHora hora_chart method 1 = __parivritti_even_reverse(dvf=2)',     'Special', 'HoraD2UmaShambu'),
    ( 3, 'D3',    3,  'ParasaraTraditional', 'BPHS Drekkana 1/5/9; PyJHora _drekkana_chart_parasara',            'Linear',  'DrekkanaD3'),
    ( 4, 'D4',    4,  'ParasaraTraditional', 'BPHS Chaturthamsa; PyJHora _chaturthamsa_parasara',                'Linear',  'ChaturthamsaD4'),
    ( 5, 'D5',    5,  'ParasaraTraditional', 'BPHS Panchamsa; PyJHora panchamsa_chart method 1',                 'Special', 'PanchamsaD5'),
    ( 6, 'D6',    6,  'ParasaraTraditional', 'BPHS Shashtamsa; AstroMath.GetShashtamsaSign',                     'Special', 'ShashtamsaD6'),
    ( 7, 'D7',    7,  'ParasaraTraditional', 'BPHS Saptamsa odd-self/even-7th; PyJHora saptamsa_chart method 1', 'Special', 'SaptamsaD7'),
    ( 8, 'D8',    8,  'ParasaraTraditional', 'BPHS Ashtamsa; PyJHora ashtamsa_chart method 1',                   'Special', 'AshtamsaD8'),
    ( 9, 'D9',    9,  'ParasaraTraditional', 'BPHS Navamsa; AstroMath.GetNavamsaSign',                           'Special', 'NavamsaD9'),
    (10, 'D10',   10, 'ParasaraTraditional', 'BPHS Dasamsa odd-self/even-9th; AstroMath.GetDasamsaSign',         'Special', 'DasamsaD10'),
    (11, 'D11',   11, 'SanjayRath',          'Sanjay Rath Rudramsa; AstroMath.GetRudramsaSign',                  'Special', 'RudramsaD11'),
    (12, 'D12',   12, 'ParasaraTraditional', 'BPHS Dwadasamsa 12-from-self; PyJHora dwadasamsa_chart method 1',  'Linear',  'DwadasamsaD12'),
    (13, 'D16',   16, 'ParasaraTraditional', 'BPHS Shodasamsa; PyJHora shodasamsa_chart method 1',               'Special', 'ShodasamsaD16'),
    (14, 'D20',   20, 'ParasaraTraditional', 'BPHS Vimsamsa; PyJHora vimsamsa_chart method 1',                   'Special', 'VimsamsaD20'),
    (15, 'D24',   24, 'ParasaraTraditional', 'BPHS Siddhamsa odd-Le/even-Cn; PyJHora chaturvimsamsa_chart m1',   'Special', 'SiddhamsaD24'),
    (16, 'D27',   27, 'ParasaraTraditional', 'BPHS Nakshatramsa by element; PyJHora nakshatramsa_chart m1',      'Special', 'NakshatramsaD27'),
    (17, 'D30',   30, 'ParasaraTraditional', 'BPHS Trimsamsa unequal 5-part; PyJHora trimsamsa_chart method 1',  'Special', 'TrimsamsaD30'),
    (18, 'D40',   40, 'ParasaraTraditional', 'BPHS Khavedamsa odd-Ar/even-Li; PyJHora khavedamsa_chart m1',      'Special', 'KhavedamsaD40'),
    (19, 'D45',   45, 'ParasaraTraditional', 'BPHS Akshavedamsa; PyJHora akshavedamsa_chart method 1',           'Special', 'AkshavedamsaD45'),
    (20, 'D60',   60, 'ParasaraTraditional', 'BPHS Shashtyamsa from-sign; PyJHora shashtyamsa_chart method 1',   'Linear',  'ShashtyamsaD60')
) AS v(Id, Code, N, MethodCode, MethodSource, Kind, SignRuleKey)
JOIN dbo.tbl_Dim_ChartType ct ON ct.Code = v.Code;
GO

-- =====================================================================
-- Planetary-state layer, slice 1 (Baaladi + Jagradadi avasthas) — star-schema (STANDARDS.md §D.1)
--   tbl_Dim_PlanetaryState / tbl_Rule_AgeState / tbl_Rule_WakefulnessState / tbl_Fact_PlanetaryState
-- Written by ChartGenerationService via PlanetaryStateComputer; read via vw_Chart_Consolidated
-- (AgeState / AgeEffectFraction / WakefulnessState). Idempotent; also shipped as the
-- one-off db/00_add_avastha_star_schema.sql for existing databases.
-- =====================================================================
IF OBJECT_ID('dbo.tbl_Dim_PlanetaryState', 'U') IS NULL
CREATE TABLE dbo.tbl_Dim_PlanetaryState (
    Id            TINYINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Dim_PlanetaryState PRIMARY KEY,
    AvasthaSystem VARCHAR(12)  NOT NULL,
    StateName     VARCHAR(20)  NOT NULL,
    SequenceOrder TINYINT      NOT NULL,
    Meaning       VARCHAR(200) NULL,
    CONSTRAINT UQ_Dim_PlanetaryState UNIQUE (AvasthaSystem, StateName)
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_PlanetaryState)
INSERT dbo.tbl_Dim_PlanetaryState (AvasthaSystem, StateName, SequenceOrder, Meaning) VALUES
    ('Baaladi',   'Baala',    1, 'Infant — quarter effect'),
    ('Baaladi',   'Kumara',   2, 'Child — half effect'),
    ('Baaladi',   'Yuva',     3, 'Youth — full effect'),
    ('Baaladi',   'Vriddha',  4, 'Old — feeble effect'),
    ('Baaladi',   'Mrita',    5, 'Dead — no effect'),
    ('Jagradadi', 'Jagrat',   1, 'Awake — full result (own sign / exaltation / moolatrikona)'),
    ('Jagradadi', 'Swapna',   2, 'Dreaming — middling result (friendly / neutral sign)'),
    ('Jagradadi', 'Sushupti', 3, 'Sleeping — weak result (enemy sign / debilitation)');
GO
IF OBJECT_ID('dbo.tbl_Rule_AgeState', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_AgeState (
    Id                 TINYINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rule_AgeState PRIMARY KEY,
    RuleSetId          TINYINT      NOT NULL CONSTRAINT FK_Rule_AgeState_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
    AvasthaStateId     TINYINT      NOT NULL CONSTRAINT FK_Rule_AgeState_State   FOREIGN KEY REFERENCES dbo.tbl_Dim_PlanetaryState (Id),
    OddSignFromDegree  DECIMAL(4,1) NOT NULL,
    OddSignToDegree    DECIMAL(4,1) NOT NULL,
    EvenSignFromDegree DECIMAL(4,1) NOT NULL,
    EvenSignToDegree   DECIMAL(4,1) NOT NULL,
    EffectFraction     DECIMAL(4,3) NOT NULL,
    MethodCode           VARCHAR(30)   NULL,
    RuleParametersJson   NVARCHAR(MAX) NULL,
    CalculationNarrative NVARCHAR(MAX) NULL,
    SourceRefCode        VARCHAR(40)   NULL,
    IsActive             BIT NOT NULL CONSTRAINT DF_Rule_AgeState_IsActive DEFAULT 1,
    CONSTRAINT UQ_Rule_AgeState UNIQUE (RuleSetId, AvasthaStateId),
    CONSTRAINT CK_RuleAgeState_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
    CONSTRAINT CK_RuleAgeState_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_AgeState)
INSERT dbo.tbl_Rule_AgeState (RuleSetId, AvasthaStateId, OddSignFromDegree, OddSignToDegree, EvenSignFromDegree, EvenSignToDegree, EffectFraction)
SELECT 1, s.Id, v.OddFrom, v.OddTo, v.EvenFrom, v.EvenTo, v.Frac
FROM (VALUES
    ('Baala',    0.0,  6.0, 24.0, 30.0, 0.250),
    ('Kumara',   6.0, 12.0, 18.0, 24.0, 0.500),
    ('Yuva',    12.0, 18.0, 12.0, 18.0, 1.000),
    ('Vriddha', 18.0, 24.0,  6.0, 12.0, 0.125),
    ('Mrita',   24.0, 30.0,  0.0,  6.0, 0.000)
) v (StateName, OddFrom, OddTo, EvenFrom, EvenTo, Frac)
JOIN dbo.tbl_Dim_PlanetaryState s ON s.AvasthaSystem = 'Baaladi' AND s.StateName = v.StateName;
GO
IF OBJECT_ID('dbo.tbl_Rule_WakefulnessState', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_WakefulnessState (
    Id             TINYINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rule_WakefulnessState PRIMARY KEY,
    RuleSetId      TINYINT     NOT NULL CONSTRAINT FK_Rule_WakefulnessState_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
    DignityStatus  VARCHAR(20) NOT NULL,
    AvasthaStateId TINYINT     NOT NULL CONSTRAINT FK_Rule_WakefulnessState_State   FOREIGN KEY REFERENCES dbo.tbl_Dim_PlanetaryState (Id),
    MethodCode           VARCHAR(30)   NULL,
    RuleParametersJson   NVARCHAR(MAX) NULL,
    CalculationNarrative NVARCHAR(MAX) NULL,
    SourceRefCode        VARCHAR(40)   NULL,
    IsActive             BIT NOT NULL CONSTRAINT DF_Rule_WakefulnessState_IsActive DEFAULT 1,
    CONSTRAINT UQ_Rule_WakefulnessState UNIQUE (RuleSetId, DignityStatus),
    CONSTRAINT CK_RuleWakeState_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
    CONSTRAINT CK_RuleWakeState_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_WakefulnessState)
INSERT dbo.tbl_Rule_WakefulnessState (RuleSetId, DignityStatus, AvasthaStateId)
SELECT 1, v.DignityStatus, s.Id
FROM (VALUES
    ('Exalted',      'Jagrat'),
    ('Moolatrikona', 'Jagrat'),
    ('Own Sign',     'Jagrat'),
    ('Great Friend', 'Swapna'),
    ('Friend',       'Swapna'),
    ('Neutral',      'Swapna'),
    ('Enemy',        'Sushupti'),
    ('Great Enemy',  'Sushupti'),
    ('Debilitated',  'Sushupti')
) v (DignityStatus, StateName)
JOIN dbo.tbl_Dim_PlanetaryState s ON s.AvasthaSystem = 'Jagradadi' AND s.StateName = v.StateName;
GO

-- =====================================================================
-- Rule-table portability (folded from db/18_rule_table_portability.sql).
-- The portability tail (MethodCode / RuleParametersJson /
-- CalculationNarrative / SourceRefCode / IsActive) + the ISJSON and
-- SRC_ CHECK constraints are defined inline in each tbl_Rule_* CREATE
-- TABLE above. This block backfills SourceRefCode for the rule tables
-- whose classical source is already known (codes resolve in
-- tbl_Dim_Source); tbl_Rule_VargaScheme stays NULL (Task 12).
-- =====================================================================
UPDATE dbo.tbl_Rule_AspectOffset                SET SourceRefCode = 'SRC_BPHS_26'         WHERE SourceRefCode IS NULL;
UPDATE dbo.tbl_Rule_CombustionOrb               SET SourceRefCode = 'SRC_BPHS_COMBUSTION' WHERE SourceRefCode IS NULL;
UPDATE dbo.tbl_Rule_NaturalRelationship         SET SourceRefCode = 'SRC_BPHS'            WHERE SourceRefCode IS NULL;
UPDATE dbo.tbl_Rule_TemporaryFriendshipDistance SET SourceRefCode = 'SRC_BPHS'            WHERE SourceRefCode IS NULL;
UPDATE dbo.tbl_Rule_AgeState                    SET SourceRefCode = 'SRC_BPHS_AVASTHA'    WHERE SourceRefCode IS NULL;
UPDATE dbo.tbl_Rule_WakefulnessState           SET SourceRefCode = 'SRC_BPHS_AVASTHA'    WHERE SourceRefCode IS NULL;
GO

-- =====================================================================
-- tbl_Rule_Catalog — one-page index of "what a port must reimplement":
-- one row per rule table (7 live + 5 reserved). EngineCode groups the
-- interpreter families; MethodCodes lists the method tags the engine
-- must understand. Folded from db/18_rule_table_portability.sql.
-- =====================================================================
IF OBJECT_ID('dbo.tbl_Rule_Catalog', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_Catalog (
    RuleTableName   VARCHAR(80)  CONSTRAINT PK_Rule_Catalog PRIMARY KEY,
    EngineCode      VARCHAR(30)  NOT NULL,
    MethodCodes     VARCHAR(300) NOT NULL,
    Purpose         VARCHAR(400) NOT NULL,
    IntroducedIn    VARCHAR(40)  NOT NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog)
INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
VALUES
    ('tbl_Rule_VargaScheme',                'VARGA',        'LINEAR_VARGA,TABLE_VARGA,BAND_VARGA,WRAPPED_VARGA', 'Per-rule-set mapping of each divisional chart type to its varga-sign derivation method.', 'migration 11'),
    ('tbl_Rule_AspectOffset',               'RELATIONSHIP', 'OFFSET_LIST',   'Graha drishti: the house offsets each planet aspects, including special aspects for Mars/Jupiter/Saturn.', 'baseline'),
    ('tbl_Rule_CombustionOrb',              'RELATIONSHIP', 'ORB_PAIR',      'Combustion (astangata) orb in degrees per planet, for direct and retrograde motion.', 'baseline'),
    ('tbl_Rule_NaturalRelationship',        'DIGNITY',      'MAP_LOOKUP',    'Naisargika (permanent) friendship: friend/neutral/enemy for each ordered planet pair.', 'baseline'),
    ('tbl_Rule_TemporaryFriendshipDistance','DIGNITY',      'DISTANCE_SET',  'Tatkalika (temporary) friendship: which sign-distances from a planet count as friendly.', 'baseline'),
    ('tbl_Rule_AgeState',                   'AVASTHA',      'BAND_LOOKUP',   'Baaladi avastha (infant..dead) degree bands per odd/even sign, with the effect fraction.', 'migration 00 / renamed 16'),
    ('tbl_Rule_WakefulnessState',           'AVASTHA',      'MAP_LOOKUP',    'Jagradadi avastha (awake/dreaming/sleeping) keyed by the planet''s dignity status.', 'migration 00 / renamed 16'),
    ('tbl_Rule_HouseSignification',         'HOUSE',        'MAP_LOOKUP',    'Reserved: bhava karakatvas — the significations attached to each house.', 'migration 18 (empty; P2)'),
    ('tbl_Rule_Karaka',                     'KARAKA',       'MAP_LOOKUP',    'Reserved: chara / sthira / naisargika karaka assignment schemes.', 'migration 18 (empty; P2)'),
    ('tbl_Rule_ShadbalaComponent',          'STRENGTH',     'WEIGHT_TABLE',  'Reserved: shadbala sub-component weights and maxima, in rupas.', 'migration 18 (empty; P3)'),
    ('tbl_Rule_VimsopakaWeight',            'STRENGTH',     'WEIGHT_TABLE',  'Reserved: vimsopaka bala varga-group weights per scheme (shadvarga..shodasavarga).', 'migration 18 (empty; P3)'),
    ('tbl_Rule_Yoga',                       'YOGA',         'PREDICATE_SET', 'Reserved: yoga definitions — formation predicates, cancellation rules, and result codes.', 'migration 18 (empty; P4)');
GO

-- =====================================================================
-- Reserved rule tables (empty; populated by later plans). Each carries
-- the same portability tail + ISJSON / SRC_ CHECKs. Folded from
-- db/18_rule_table_portability.sql.
-- =====================================================================
IF OBJECT_ID('dbo.tbl_Rule_HouseSignification', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_HouseSignification (
    Id INT IDENTITY(1,1) CONSTRAINT PK_Rule_HouseSignification PRIMARY KEY,
    RuleSetId INT NOT NULL,
    HouseNumber TINYINT NOT NULL,
    SignificationCode VARCHAR(40) NOT NULL,
    MethodCode VARCHAR(30) NULL,
    RuleParametersJson NVARCHAR(MAX) NULL,
    CalculationNarrative NVARCHAR(MAX) NULL,
    SourceRefCode VARCHAR(40) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Rule_HouseSignification_IsActive DEFAULT 1,
    CONSTRAINT CK_Rule_HouseSignification_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
    CONSTRAINT CK_Rule_HouseSignification_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
);
GO
IF OBJECT_ID('dbo.tbl_Rule_Karaka', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_Karaka (
    Id INT IDENTITY(1,1) CONSTRAINT PK_Rule_Karaka PRIMARY KEY,
    RuleSetId INT NOT NULL,
    KarakaScheme VARCHAR(20) NOT NULL,
    PlanetOrHouse VARCHAR(20) NULL,
    TargetValue VARCHAR(40) NULL,
    OrderIndex TINYINT NULL,
    ReverseForRahu BIT NULL,
    MethodCode VARCHAR(30) NULL,
    RuleParametersJson NVARCHAR(MAX) NULL,
    CalculationNarrative NVARCHAR(MAX) NULL,
    SourceRefCode VARCHAR(40) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Rule_Karaka_IsActive DEFAULT 1,
    CONSTRAINT CK_Rule_Karaka_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
    CONSTRAINT CK_Rule_Karaka_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
);
GO
IF OBJECT_ID('dbo.tbl_Rule_ShadbalaComponent', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_ShadbalaComponent (
    Id INT IDENTITY(1,1) CONSTRAINT PK_Rule_ShadbalaComponent PRIMARY KEY,
    RuleSetId INT NOT NULL,
    BalaCode VARCHAR(30) NOT NULL,
    SubComponentCode VARCHAR(40) NULL,
    WeightRupas DECIMAL(6,3) NULL,
    MaxRupas DECIMAL(6,3) NULL,
    LookupJson NVARCHAR(MAX) NULL,
    MethodCode VARCHAR(30) NULL,
    RuleParametersJson NVARCHAR(MAX) NULL,
    CalculationNarrative NVARCHAR(MAX) NULL,
    SourceRefCode VARCHAR(40) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Rule_ShadbalaComponent_IsActive DEFAULT 1,
    CONSTRAINT CK_Rule_ShadbalaComponent_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
    CONSTRAINT CK_Rule_ShadbalaComponent_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
);
GO
IF OBJECT_ID('dbo.tbl_Rule_VimsopakaWeight', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_VimsopakaWeight (
    Id INT IDENTITY(1,1) CONSTRAINT PK_Rule_VimsopakaWeight PRIMARY KEY,
    RuleSetId INT NOT NULL,
    SchemeCode VARCHAR(20) NOT NULL,
    VargaChartType VARCHAR(10) NOT NULL,
    Weight DECIMAL(5,2) NULL,
    MaxTotal DECIMAL(6,2) NULL,
    MethodCode VARCHAR(30) NULL,
    RuleParametersJson NVARCHAR(MAX) NULL,
    CalculationNarrative NVARCHAR(MAX) NULL,
    SourceRefCode VARCHAR(40) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Rule_VimsopakaWeight_IsActive DEFAULT 1,
    CONSTRAINT CK_Rule_VimsopakaWeight_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
    CONSTRAINT CK_Rule_VimsopakaWeight_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
);
GO
IF OBJECT_ID('dbo.tbl_Rule_Yoga', 'U') IS NULL
CREATE TABLE dbo.tbl_Rule_Yoga (
    Id INT IDENTITY(1,1) CONSTRAINT PK_Rule_Yoga PRIMARY KEY,
    RuleSetId INT NOT NULL,
    YogaCode VARCHAR(40) NOT NULL,
    YogaCategory VARCHAR(30) NULL,
    RequirementJson NVARCHAR(MAX) NULL,
    CancellationJson NVARCHAR(MAX) NULL,
    ResultCode VARCHAR(40) NULL,
    MethodCode VARCHAR(30) NULL,
    RuleParametersJson NVARCHAR(MAX) NULL,
    CalculationNarrative NVARCHAR(MAX) NULL,
    SourceRefCode VARCHAR(40) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Rule_Yoga_IsActive DEFAULT 1,
    CONSTRAINT CK_Rule_Yoga_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
    CONSTRAINT CK_Rule_Yoga_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
);
GO
IF OBJECT_ID('dbo.tbl_Fact_PlanetaryState', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Fact_PlanetaryState (
        Id                    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Fact_PlanetaryState PRIMARY KEY,
        ChartResultId         INT           NOT NULL CONSTRAINT FK_Fact_PlanetaryState_ChartResult FOREIGN KEY REFERENCES dbo.tbl_ChartResults (Id),
        Planet                VARCHAR(20)   NOT NULL,
        RuleSetId             TINYINT       NOT NULL CONSTRAINT FK_Fact_PlanetaryState_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        AgeStateId            TINYINT       NULL CONSTRAINT FK_Fact_PlanetaryState_Age   FOREIGN KEY REFERENCES dbo.tbl_Dim_PlanetaryState (Id),
        AgeEffectFraction     DECIMAL(4,3)  NULL,
        WakefulnessStateId    TINYINT       NULL CONSTRAINT FK_Fact_PlanetaryState_Wakefulness FOREIGN KEY REFERENCES dbo.tbl_Dim_PlanetaryState (Id),
        PlanetId              TINYINT       NULL CONSTRAINT FK_Fact_PlanetaryState_Planet FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        ChartTypeId           TINYINT       NULL,
        CONSTRAINT UQ_Fact_PlanetaryState UNIQUE (ChartResultId, Planet)
    );
    CREATE NONCLUSTERED INDEX IX_Fact_PlanetaryState_ChartResultId ON dbo.tbl_Fact_PlanetaryState (ChartResultId);
END
GO

-- =====================================================================
-- vw_Chart_HouseNakshatraSpan — defined here (not with the other views near
-- the top): after migration 09 it joins tbl_ChartResults + tbl_Dim_ChartType,
-- both created later than the original position.
-- =====================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_Chart_HouseNakshatraSpan]'))
EXEC dbo.sp_executesql @statement = N'
CREATE VIEW [dbo].[vw_Chart_HouseNakshatraSpan] AS
SELECT
    hl.ChartResultId,
    cr.BirthDetailId,
    ct.Code                    AS ChartType,
    hl.HouseNumber,
    hl.HouseSign,
    sa.Id                      AS HouseSignId,
    hl.LordPlanet,
    n.Id                       AS NakshatraId,
    n.NakshatraName,
    p.PadaNumber,
    p.StartDegree              AS PadaStartDegree,
    p.EndDegree                AS PadaEndDegree,
    lord.PlanetName            AS NakshatraLordName,
    nav.SignName               AS NavamsaSignName
FROM dbo.tbl_Chart_HouseLords hl
JOIN dbo.tbl_ChartResults    cr   ON cr.Id = hl.ChartResultId
JOIN dbo.tbl_Dim_ChartType   ct   ON ct.Id = cr.ChartTypeId
JOIN dbo.tbl_SignAttributes  sa   ON sa.Id = hl.HouseSignId
JOIN dbo.tbl_NakshatraPadas  p    ON p.StartDegree >= (sa.Id - 1) * 30.0
                                 AND p.StartDegree <  sa.Id * 30.0
JOIN dbo.tbl_Nakshatras      n    ON n.Id = p.NakshatraId
JOIN dbo.tbl_Planets         lord ON lord.Id = n.RulingPlanetId
JOIN dbo.tbl_SignAttributes  nav  ON nav.Id = p.NavamsaSignId;
'
GO

-- =====================================================================
-- vw_Chart_Consolidated — defined last: it reads tbl_Fact_PlanetaryState /
-- tbl_Dim_PlanetaryState (created just above) as well as the tbl_Chart_* tables.
-- =====================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO
IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[dbo].[vw_Chart_Consolidated]'))
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
    kd.PointKind,
    kd.CharaKaraka,
    kd.NirayanaLongitudeDegrees,
    kd.VargaLongitudeDegrees,
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
    ageState.StateName           AS AgeState,
    av.AgeEffectFraction,
    wakeState.StateName          AS WakefulnessState,
    RulesHouses.HouseList        AS RulesHouseNumbers,
    Conjunct.PlanetList           AS ConjunctWith,
    AspectsCast.TargetList        AS Aspects,
    kd.AspectingPlanets          AS AspectedBy,
    cr.ComputedAt
FROM dbo.tbl_Chart_KeyDetails kd
JOIN dbo.tbl_ChartResults cr  ON cr.Id = kd.ChartResultId
JOIN dbo.tbl_BirthDetails bd  ON bd.Id = cr.BirthDetailId
LEFT JOIN dbo.tbl_Fact_PlanetaryState av ON av.ChartResultId = kd.ChartResultId AND av.PlanetId = kd.PlanetId
LEFT JOIN dbo.tbl_Dim_PlanetaryState  ageState  ON ageState.Id  = av.AgeStateId
LEFT JOIN dbo.tbl_Dim_PlanetaryState  wakeState ON wakeState.Id = av.WakefulnessStateId
OUTER APPLY (
    SELECT STRING_AGG(CAST(hl.HouseNumber AS VARCHAR(2)), '','') WITHIN GROUP (ORDER BY hl.HouseNumber) AS HouseList
    FROM dbo.tbl_Chart_HouseLords hl
    WHERE hl.ChartResultId = kd.ChartResultId AND hl.LordPlanetId = kd.PlanetId
) RulesHouses
OUTER APPLY (
    SELECT STRING_AGG(other_planet, '', '') AS PlanetList
    FROM (
        SELECT Planet2 AS other_planet FROM dbo.tbl_Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet1Id = kd.PlanetId
        UNION ALL
        SELECT Planet1 FROM dbo.tbl_Chart_Conjunctions
            WHERE ChartResultId = kd.ChartResultId AND Planet2Id = kd.PlanetId
    ) x
) Conjunct
OUTER APPLY (
    SELECT STRING_AGG(CONCAT(AspectedTarget, '' ('', AspectType, '')''), '', '') AS TargetList
    FROM dbo.tbl_Chart_Aspects
    WHERE ChartResultId = kd.ChartResultId AND AspectingPlanetId = kd.PlanetId
) AspectsCast;
' 
GO
