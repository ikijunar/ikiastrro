-- vedic_horo_gen: Name-first column ordering + AspectingPlanets on tbl_D1Chart_keydetails
--
-- SQL Server has no in-place column reorder — the only way to physically move a column is to
-- rebuild the table (new table, copy data, drop old, rename). Scoped to the 4 tbl_D1Chart_* tables
-- only (not BirthDetails/ChartResults — those are FK parents referenced everywhere; Id-first is the
-- standard convention there and reordering them is materially higher risk for a cosmetic change).
--
-- Safe here because these 4 tables are leaves (nothing else has an FK into them) — only the
-- consolidated view depends on them, so it's dropped first and recreated at the end.
USE vedic_horo_gen;
GO

DROP VIEW IF EXISTS dbo.vw_D1Chart_Consolidated;
GO

-- 1) tbl_D1Chart_keydetails — Name first, + new AspectingPlanets column
IF COL_LENGTH('dbo.tbl_D1Chart_keydetails', 'AspectingPlanets') IS NULL
   OR (SELECT TOP 1 column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_D1Chart_keydetails') AND name = 'Name') <> 1
BEGIN
    CREATE TABLE dbo.tbl_D1Chart_keydetails_new
    (
        Name                    NVARCHAR(200)   NULL,
        Id                      INT IDENTITY(1,1) PRIMARY KEY,
        ChartResultId           INT             NOT NULL REFERENCES dbo.tbl_ChartResults(Id),
        BirthDetailId           INT             NOT NULL REFERENCES dbo.tbl_BirthDetails(Id),
        Planet                  VARCHAR(20)     NOT NULL,
        Sign                    VARCHAR(20)     NOT NULL,
        DegreesInSignDisplay    VARCHAR(20)     NOT NULL,
        DegreesInSignDecimal    DECIMAL(7,4)    NOT NULL,
        NirayanaLongitudeDegrees FLOAT          NOT NULL,
        Nakshatra               VARCHAR(20)     NULL,
        NakshatraPada           TINYINT         NULL,
        HouseNumberFromLagna    TINYINT         NOT NULL,
        HouseNumberFromSun      TINYINT         NOT NULL,
        HouseNumberFromMoon     TINYINT         NOT NULL,
        OwnSigns                VARCHAR(30)     NULL,
        ExaltationSign          VARCHAR(20)     NULL,
        DebilitationSign        VARCHAR(20)     NULL,
        MoolatrikonaSign        VARCHAR(20)     NULL,
        MoolatrikonaRange       VARCHAR(20)     NULL,
        SignLordPlanet          VARCHAR(20)     NULL,
        DignityStatus           VARCHAR(20)     NULL,
        AspectingPlanets        VARCHAR(200)    NULL,   -- e.g. 'Mars (8th), Saturn (3rd)' — who aspects this planet
        ComputedAt              DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );

    INSERT INTO dbo.tbl_D1Chart_keydetails_new
        (Name, ChartResultId, BirthDetailId, Planet, Sign, DegreesInSignDisplay, DegreesInSignDecimal,
         NirayanaLongitudeDegrees, Nakshatra, NakshatraPada,
         HouseNumberFromLagna, HouseNumberFromSun, HouseNumberFromMoon,
         OwnSigns, ExaltationSign, DebilitationSign, MoolatrikonaSign, MoolatrikonaRange,
         SignLordPlanet, DignityStatus, AspectingPlanets, ComputedAt)
    SELECT
        kd.Name, kd.ChartResultId, kd.BirthDetailId, kd.Planet, kd.Sign, kd.DegreesInSignDisplay, kd.DegreesInSignDecimal,
        kd.NirayanaLongitudeDegrees, kd.Nakshatra, kd.NakshatraPada,
        kd.HouseNumberFromLagna, kd.HouseNumberFromSun, kd.HouseNumberFromMoon,
        kd.OwnSigns, kd.ExaltationSign, kd.DebilitationSign, kd.MoolatrikonaSign, kd.MoolatrikonaRange,
        kd.SignLordPlanet, kd.DignityStatus,
        Asp.AspectingList,
        kd.ComputedAt
    FROM dbo.tbl_D1Chart_keydetails kd
    OUTER APPLY (
        SELECT STRING_AGG(CONCAT(a.AspectingPlanet, ' (', a.AspectType, ')'), ', ') AS AspectingList
        FROM dbo.tbl_D1Chart_Aspects a
        WHERE a.ChartResultId = kd.ChartResultId AND a.AspectedTarget = kd.Planet
    ) Asp;

    DROP TABLE dbo.tbl_D1Chart_keydetails;
    EXEC sp_rename 'dbo.tbl_D1Chart_keydetails_new', 'tbl_D1Chart_keydetails';

    CREATE INDEX IX_D1KeyDetails_ChartResultId ON dbo.tbl_D1Chart_keydetails (ChartResultId);
    CREATE INDEX IX_D1KeyDetails_BirthDetailId_Planet ON dbo.tbl_D1Chart_keydetails (BirthDetailId, Planet);
END
GO

-- 2) tbl_D1Chart_HouseLords — Name first
IF (SELECT TOP 1 column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_D1Chart_HouseLords') AND name = 'Name') <> 1
BEGIN
    CREATE TABLE dbo.tbl_D1Chart_HouseLords_new
    (
        Name                NVARCHAR(200)   NULL,
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        ChartResultId       INT             NOT NULL REFERENCES dbo.tbl_ChartResults(Id),
        BirthDetailId       INT             NOT NULL REFERENCES dbo.tbl_BirthDetails(Id),
        HouseNumber         TINYINT         NOT NULL,
        HouseSign           VARCHAR(20)     NOT NULL,
        LordPlanet          VARCHAR(20)     NOT NULL,
        LordPlacedInHouseFromLagna TINYINT  NOT NULL,
        LordPlacedInHouseFromSun   TINYINT  NOT NULL,
        LordPlacedInHouseFromMoon  TINYINT  NOT NULL,
        LordPlacedInSign    VARCHAR(20)     NOT NULL,
        LordDignityStatus   VARCHAR(20)     NULL,
        ComputedAt          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );

    INSERT INTO dbo.tbl_D1Chart_HouseLords_new
        (Name, ChartResultId, BirthDetailId, HouseNumber, HouseSign, LordPlanet,
         LordPlacedInHouseFromLagna, LordPlacedInHouseFromSun, LordPlacedInHouseFromMoon,
         LordPlacedInSign, LordDignityStatus, ComputedAt)
    SELECT Name, ChartResultId, BirthDetailId, HouseNumber, HouseSign, LordPlanet,
           LordPlacedInHouseFromLagna, LordPlacedInHouseFromSun, LordPlacedInHouseFromMoon,
           LordPlacedInSign, LordDignityStatus, ComputedAt
    FROM dbo.tbl_D1Chart_HouseLords;

    DROP TABLE dbo.tbl_D1Chart_HouseLords;
    EXEC sp_rename 'dbo.tbl_D1Chart_HouseLords_new', 'tbl_D1Chart_HouseLords';

    CREATE INDEX IX_D1HouseLords_ChartResultId ON dbo.tbl_D1Chart_HouseLords (ChartResultId);
    CREATE UNIQUE INDEX UX_D1HouseLords_ChartResultId_HouseNumber
        ON dbo.tbl_D1Chart_HouseLords (ChartResultId, HouseNumber);
END
GO

-- 3) tbl_D1Chart_Conjunctions — Name first
IF (SELECT TOP 1 column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_D1Chart_Conjunctions') AND name = 'Name') <> 1
BEGIN
    CREATE TABLE dbo.tbl_D1Chart_Conjunctions_new
    (
        Name                    NVARCHAR(200)   NULL,
        Id                      INT IDENTITY(1,1) PRIMARY KEY,
        ChartResultId           INT             NOT NULL REFERENCES dbo.tbl_ChartResults(Id),
        BirthDetailId           INT             NOT NULL REFERENCES dbo.tbl_BirthDetails(Id),
        Planet1                 VARCHAR(20)     NOT NULL,
        Planet2                 VARCHAR(20)     NOT NULL,
        Sign                    VARCHAR(20)     NOT NULL,
        HouseNumberFromLagna    TINYINT         NOT NULL,
        DegreeSeparation        DECIMAL(7,4)    NOT NULL,
        ComputedAt              DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );

    INSERT INTO dbo.tbl_D1Chart_Conjunctions_new
        (Name, ChartResultId, BirthDetailId, Planet1, Planet2, Sign, HouseNumberFromLagna, DegreeSeparation, ComputedAt)
    SELECT Name, ChartResultId, BirthDetailId, Planet1, Planet2, Sign, HouseNumberFromLagna, DegreeSeparation, ComputedAt
    FROM dbo.tbl_D1Chart_Conjunctions;

    DROP TABLE dbo.tbl_D1Chart_Conjunctions;
    EXEC sp_rename 'dbo.tbl_D1Chart_Conjunctions_new', 'tbl_D1Chart_Conjunctions';

    CREATE INDEX IX_D1Conjunctions_ChartResultId ON dbo.tbl_D1Chart_Conjunctions (ChartResultId);
END
GO

-- 4) tbl_D1Chart_Aspects — Name first
IF (SELECT TOP 1 column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_D1Chart_Aspects') AND name = 'Name') <> 1
BEGIN
    CREATE TABLE dbo.tbl_D1Chart_Aspects_new
    (
        Name                NVARCHAR(200)   NULL,
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        ChartResultId       INT             NOT NULL REFERENCES dbo.tbl_ChartResults(Id),
        BirthDetailId       INT             NOT NULL REFERENCES dbo.tbl_BirthDetails(Id),
        AspectingPlanet     VARCHAR(20)     NOT NULL,
        AspectedTarget      VARCHAR(20)     NOT NULL,
        AspectType          VARCHAR(10)     NOT NULL,
        ComputedAt          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );

    INSERT INTO dbo.tbl_D1Chart_Aspects_new
        (Name, ChartResultId, BirthDetailId, AspectingPlanet, AspectedTarget, AspectType, ComputedAt)
    SELECT Name, ChartResultId, BirthDetailId, AspectingPlanet, AspectedTarget, AspectType, ComputedAt
    FROM dbo.tbl_D1Chart_Aspects;

    DROP TABLE dbo.tbl_D1Chart_Aspects;
    EXEC sp_rename 'dbo.tbl_D1Chart_Aspects_new', 'tbl_D1Chart_Aspects';

    CREATE INDEX IX_D1Aspects_ChartResultId ON dbo.tbl_D1Chart_Aspects (ChartResultId);
END
GO
