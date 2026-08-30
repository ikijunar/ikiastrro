-- vedic_horo_gen: Lagna/Sun/Moon house reckoning + conjunctions + aspects
USE vedic_horo_gen;
GO

-- 1) Rename HouseNumber -> HouseNumberFromLagna, add FromSun/FromMoon variants
IF COL_LENGTH('dbo.tbl_D1Chart_keydetails', 'HouseNumber') IS NOT NULL
   AND COL_LENGTH('dbo.tbl_D1Chart_keydetails', 'HouseNumberFromLagna') IS NULL
BEGIN
    EXEC sp_rename 'dbo.tbl_D1Chart_keydetails.HouseNumber', 'HouseNumberFromLagna', 'COLUMN';
END
GO

IF COL_LENGTH('dbo.tbl_D1Chart_keydetails', 'HouseNumberFromSun') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_D1Chart_keydetails ADD HouseNumberFromSun TINYINT NULL;
END
GO

IF COL_LENGTH('dbo.tbl_D1Chart_keydetails', 'HouseNumberFromMoon') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_D1Chart_keydetails ADD HouseNumberFromMoon TINYINT NULL;
END
GO

-- 2) Rename LordPlacedInHouse -> LordPlacedInHouseFromLagna, add FromSun/FromMoon variants
IF COL_LENGTH('dbo.tbl_D1Chart_HouseLords', 'LordPlacedInHouse') IS NOT NULL
   AND COL_LENGTH('dbo.tbl_D1Chart_HouseLords', 'LordPlacedInHouseFromLagna') IS NULL
BEGIN
    EXEC sp_rename 'dbo.tbl_D1Chart_HouseLords.LordPlacedInHouse', 'LordPlacedInHouseFromLagna', 'COLUMN';
END
GO

IF COL_LENGTH('dbo.tbl_D1Chart_HouseLords', 'LordPlacedInHouseFromSun') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_D1Chart_HouseLords ADD LordPlacedInHouseFromSun TINYINT NULL;
END
GO

IF COL_LENGTH('dbo.tbl_D1Chart_HouseLords', 'LordPlacedInHouseFromMoon') IS NULL
BEGIN
    ALTER TABLE dbo.tbl_D1Chart_HouseLords ADD LordPlacedInHouseFromMoon TINYINT NULL;
END
GO

-- 3) Backfill FromSun/FromMoon for any pre-existing rows (sign-order lookup done inline; SQL Server has
--    no built-in "count from sign to sign", so this mirrors AstronomicalCalculator.CountFromSignToSign:
--    same sign = 1, then +1 per sign forward, wrapping at 12).
;WITH SignIndex AS (
    SELECT * FROM (VALUES
        ('Aries',1),('Taurus',2),('Gemini',3),('Cancer',4),('Leo',5),('Virgo',6),
        ('Libra',7),('Scorpio',8),('Sagittarius',9),('Capricornus',10),('Aquarius',11),('Pisces',12)
    ) AS s(SignName, SignOrder)
),
SunMoonSign AS (
    SELECT kd.ChartResultId,
           MAX(CASE WHEN kd.Planet = 'Sun' THEN si.SignOrder END) AS SunOrder,
           MAX(CASE WHEN kd.Planet = 'Moon' THEN si.SignOrder END) AS MoonOrder
    FROM dbo.tbl_D1Chart_keydetails kd
    JOIN SignIndex si ON si.SignName = kd.Sign
    GROUP BY kd.ChartResultId
)
UPDATE kd
SET kd.HouseNumberFromSun  = ((si.SignOrder - sm.SunOrder + 12) % 12) + 1,
    kd.HouseNumberFromMoon = ((si.SignOrder - sm.MoonOrder + 12) % 12) + 1
FROM dbo.tbl_D1Chart_keydetails kd
JOIN SignIndex si ON si.SignName = kd.Sign
JOIN SunMoonSign sm ON sm.ChartResultId = kd.ChartResultId
WHERE kd.HouseNumberFromSun IS NULL OR kd.HouseNumberFromMoon IS NULL;
GO

UPDATE hl
SET hl.LordPlacedInHouseFromSun  = kd.HouseNumberFromSun,
    hl.LordPlacedInHouseFromMoon = kd.HouseNumberFromMoon
FROM dbo.tbl_D1Chart_HouseLords hl
JOIN dbo.tbl_D1Chart_keydetails kd
    ON kd.ChartResultId = hl.ChartResultId AND kd.Planet = hl.LordPlanet
WHERE hl.LordPlacedInHouseFromSun IS NULL OR hl.LordPlacedInHouseFromMoon IS NULL;
GO

-- Now that every row is backfilled, make the three reckoning columns NOT NULL going forward
ALTER TABLE dbo.tbl_D1Chart_keydetails ALTER COLUMN HouseNumberFromSun TINYINT NOT NULL;
ALTER TABLE dbo.tbl_D1Chart_keydetails ALTER COLUMN HouseNumberFromMoon TINYINT NOT NULL;
ALTER TABLE dbo.tbl_D1Chart_HouseLords ALTER COLUMN LordPlacedInHouseFromSun TINYINT NOT NULL;
ALTER TABLE dbo.tbl_D1Chart_HouseLords ALTER COLUMN LordPlacedInHouseFromMoon TINYINT NOT NULL;
GO

-- 4) Conjunctions (Graha Yuti) — pairs of grahas sharing the same D1 sign
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_D1Chart_Conjunctions')
BEGIN
    CREATE TABLE dbo.tbl_D1Chart_Conjunctions
    (
        Name                    NVARCHAR(200)   NULL,       -- deliberately 1st column
        Id                      INT IDENTITY(1,1) PRIMARY KEY,
        ChartResultId           INT             NOT NULL REFERENCES dbo.tbl_ChartResults(Id),
        BirthDetailId           INT             NOT NULL REFERENCES dbo.tbl_BirthDetails(Id),
        Planet1                 VARCHAR(20)     NOT NULL,
        Planet2                 VARCHAR(20)     NOT NULL,
        Sign                    VARCHAR(20)     NOT NULL,
        HouseNumberFromLagna    TINYINT         NOT NULL,
        DegreeSeparation        DECIMAL(7,4)    NOT NULL,   -- 0-180, how tight the conjunction is
        ComputedAt              DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_D1Conjunctions_ChartResultId ON dbo.tbl_D1Chart_Conjunctions (ChartResultId);
END
GO

-- 5) Aspects (Graha Drishti) — directional planet-to-(planet-or-Ascendant)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_D1Chart_Aspects')
BEGIN
    CREATE TABLE dbo.tbl_D1Chart_Aspects
    (
        Name                NVARCHAR(200)   NULL,       -- deliberately 1st column
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        ChartResultId       INT             NOT NULL REFERENCES dbo.tbl_ChartResults(Id),
        BirthDetailId       INT             NOT NULL REFERENCES dbo.tbl_BirthDetails(Id),
        AspectingPlanet     VARCHAR(20)     NOT NULL,
        AspectedTarget      VARCHAR(20)     NOT NULL,   -- a graha name, or 'Ascendant'
        AspectType          VARCHAR(10)     NOT NULL,   -- '7th','4th','8th','5th','9th','3rd','10th'
        ComputedAt          DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_D1Aspects_ChartResultId ON dbo.tbl_D1Chart_Aspects (ChartResultId);
END
GO
