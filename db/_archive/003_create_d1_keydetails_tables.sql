-- vedic_horo_gen: D1 key-details tables
-- Flattens ChartResults.ResultJson (D1 only) into queryable columns, plus classical
-- planetary dignity (exaltation/debilitation/Panchadha Maitri) and house-lordship analysis.
--
-- NOTE: this file is the ORIGINAL 2026-08-24 shape and is NOT the current schema. Later
-- migrations supersede it — 010 (rename tbl_D1Chart_* -> tbl_Chart_*, + ChartType, generalized
-- to every chart type), 014 (schema review: drop Name), 015 (IsRetrograde/IsCombust/
-- DistanceFromSunDegrees/CombustionOrbUsedDegrees/NakshatraLordPlanet), 032 (NakshatraId/
-- NakshatraPadaId/NakshatraSubLordPlanet). A fresh install runs 001..NNN in sequence, so no
-- rewrite of this file is needed; the live DB / the latest migration is the authoritative shape.
--
-- Design: two tables, not one.
--   tbl_D1Chart_keydetails = one row per planet (incl. Ascendant), 10 rows per D1 chart.
--   tbl_D1Chart_HouseLords = one row per house (1-12) per D1 chart, since a planet can rule
--     0, 1, or 2 houses depending on the Ascendant sign - that doesn't fit cleanly as a
--     column on the planet-grain table.
USE vedic_horo_gen;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_D1Chart_keydetails')
BEGIN
    CREATE TABLE dbo.tbl_D1Chart_keydetails
    (
        Name                    NVARCHAR(200)   NULL,       -- denormalized from BirthDetails, so this table reads standalone — deliberately 1st column
        Id                      INT IDENTITY(1,1) PRIMARY KEY,
        ChartResultId           INT             NOT NULL REFERENCES dbo.tbl_ChartResults(Id),
        BirthDetailId           INT             NOT NULL REFERENCES dbo.tbl_BirthDetails(Id),
        Planet                  VARCHAR(20)     NOT NULL,   -- 'Ascendant','Sun',...,'Rahu','Ketu'
        Sign                    VARCHAR(20)     NOT NULL,
        DegreesInSignDisplay    VARCHAR(20)     NOT NULL,   -- e.g. 0°38'20" (as stored in the JSON)
        DegreesInSignDecimal    DECIMAL(7,4)    NOT NULL,   -- same value, numeric: NirayanaLongitude % 30
        NirayanaLongitudeDegrees FLOAT          NOT NULL,
        Nakshatra               VARCHAR(20)     NULL,       -- NULL for D9-only-style entries; always populated for D1
        NakshatraPada           TINYINT         NULL,
        HouseNumberFromLagna    TINYINT         NOT NULL,   -- 1-12, Whole Sign from Ascendant (default reckoning)
        HouseNumberFromSun      TINYINT         NOT NULL,   -- 1-12, Whole Sign from the Sun's sign (Surya Lagna)
        HouseNumberFromMoon     TINYINT         NOT NULL,   -- 1-12, Whole Sign from the Moon's sign (Chandra Lagna)

        -- Classical dignity (NULL for the Ascendant row - Lagna itself has no dignity concept)
        OwnSigns                VARCHAR(30)     NULL,       -- e.g. 'Aries, Scorpio' for Mars
        ExaltationSign          VARCHAR(20)     NULL,
        DebilitationSign        VARCHAR(20)     NULL,
        MoolatrikonaSign        VARCHAR(20)     NULL,
        MoolatrikonaRange       VARCHAR(20)     NULL,       -- e.g. '0-12' (degrees within MoolatrikonaSign)
        SignLordPlanet          VARCHAR(20)     NULL,       -- ruler of the sign this row's Planet is sitting in
        DignityStatus           VARCHAR(20)     NULL,       -- Exalted / Moolatrikona / Own Sign / Great Friend /
                                                              -- Friend / Neutral / Enemy / Great Enemy / Debilitated
        AspectingPlanets        VARCHAR(200)    NULL,       -- e.g. 'Mars (8th), Saturn (3rd)' — who aspects this planet

        ComputedAt              DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_D1KeyDetails_ChartResultId ON dbo.tbl_D1Chart_keydetails (ChartResultId);
    CREATE INDEX IX_D1KeyDetails_BirthDetailId_Planet ON dbo.tbl_D1Chart_keydetails (BirthDetailId, Planet);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_D1Chart_HouseLords')
BEGIN
    CREATE TABLE dbo.tbl_D1Chart_HouseLords
    (
        Name                NVARCHAR(200)   NULL,       -- denormalized from BirthDetails, so this table reads standalone — deliberately 1st column
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        ChartResultId       INT             NOT NULL REFERENCES dbo.tbl_ChartResults(Id),
        BirthDetailId       INT             NOT NULL REFERENCES dbo.tbl_BirthDetails(Id),
        HouseNumber         TINYINT         NOT NULL,   -- 1-12
        HouseSign           VARCHAR(20)     NOT NULL,   -- sign occupying this house (Whole Sign from Ascendant)
        LordPlanet          VARCHAR(20)     NOT NULL,   -- ruler of HouseSign
        LordPlacedInHouseFromLagna TINYINT  NOT NULL,   -- which house LordPlanet sits in, from Lagna
        LordPlacedInHouseFromSun   TINYINT  NOT NULL,   -- which house LordPlanet sits in, from Surya Lagna
        LordPlacedInHouseFromMoon  TINYINT  NOT NULL,   -- which house LordPlanet sits in, from Chandra Lagna
        LordPlacedInSign    VARCHAR(20)     NOT NULL,   -- which sign LordPlanet actually sits in, this chart
        LordDignityStatus   VARCHAR(20)     NULL,       -- denormalized copy from tbl_D1Chart_keydetails,
                                                          -- for "is this house's lord well-placed" queries
                                                          -- without a join
        ComputedAt          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_D1HouseLords_ChartResultId ON dbo.tbl_D1Chart_HouseLords (ChartResultId);
    CREATE UNIQUE INDEX UX_D1HouseLords_ChartResultId_HouseNumber
        ON dbo.tbl_D1Chart_HouseLords (ChartResultId, HouseNumber);
END
GO
