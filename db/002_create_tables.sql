-- vedic_horo_gen: core tables
-- tbl_BirthDetails = raw input (never changes shape as chart types grow).
-- tbl_ChartResults = computed output, one row per (BirthDetail, ChartType, EngineVersion) —
--   new chart/calculation types later are new rows, not schema changes.
-- (Renamed from BirthDetails/ChartResults 2026-08-24 — see migration 008 for the live-DB rename.
--  This file reflects the final names directly so a fresh install doesn't need 008 at all.)
USE vedic_horo_gen;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_BirthDetails')
BEGIN
    CREATE TABLE dbo.tbl_BirthDetails
    (
        Id                    INT IDENTITY(1,1) PRIMARY KEY,
        Name                  NVARCHAR(200)   NOT NULL,
        DateOfBirth           DATE            NOT NULL,
        TimeOfBirth           TIME(0)         NOT NULL,
        CorrectedTimeOfBirth  TIME(0)         NULL,
        CorrectionReason      NVARCHAR(1000)  NULL,
        PlaceCity             NVARCHAR(200)   NOT NULL,
        PlaceCountry          NVARCHAR(200)   NOT NULL,
        Latitude              FLOAT           NOT NULL,
        Longitude             FLOAT           NOT NULL,
        UtcOffset             VARCHAR(10)     NOT NULL,   -- e.g. '05:30:00' or '-08:00:00'
        IanaTimeZoneId        VARCHAR(100)    NULL,       -- e.g. 'Asia/Kolkata'
        CreatedAt             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_ChartResults')
BEGIN
    CREATE TABLE dbo.tbl_ChartResults
    (
        Id             INT IDENTITY(1,1) PRIMARY KEY,
        BirthDetailId  INT             NOT NULL REFERENCES dbo.tbl_BirthDetails(Id),
        ChartType      NVARCHAR(50)    NOT NULL,          -- 'D1', 'D9', later: 'D2', 'VimshottariDasha', ...
        Ayanamsha      NVARCHAR(50)    NOT NULL DEFAULT 'Lahiri',
        HouseSystem    NVARCHAR(50)    NOT NULL DEFAULT 'WholeSign',
        EngineVersion  NVARCHAR(100)   NOT NULL,
        ResultJson     NVARCHAR(MAX)   NOT NULL,
        ComputedAt     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_ChartResults_BirthDetailId_ChartType ON dbo.tbl_ChartResults (BirthDetailId, ChartType);
END
GO
