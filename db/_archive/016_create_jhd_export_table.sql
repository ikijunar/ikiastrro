-- vedic_horo_gen: tbl_Chart_JhdExport — auto-computed Jagannatha Hora (.jhd) file field
-- values, one row per tbl_BirthDetails person, so manually re-deriving the .jhd degree/time
-- encoding by hand (the exact mistake that produced a wrong 5:50 AM instead of 5:30 AM in
-- Ramakrishnan's hand-built .jhd on 2026-08-28) never has to happen again.
--
-- .jhd encoding, reverse-engineered + verified against Jagannatha Hora's own bundled sample
-- charts and confirmed correct by a real save/load round-trip (Ramakrishnan.jhd, 2026-08-28):
--   - Time / TimeZone / Longitude / Latitude all use "digit-literal" DD.MM packing — the
--     digits after the decimal point ARE minutes (not a fraction of the whole), e.g. 5.30
--     means 5h30m, NOT 5h18m (5.30 hours). This is the exact bug that was just fixed by hand.
--   - TimeZone and Longitude are NEGATIVE for East of Greenwich (opposite of the standard
--     GIS/tbl_BirthDetails sign convention, where Longitude/UtcOffset are positive for East) —
--     both must be sign-flipped on the way out of tbl_BirthDetails.
--   - Latitude keeps the standard sign (positive = North), no flip.
--   - Lines 9/10 (an internal LMT-reference pair JH itself writes) are set equal to line 5
--     (TimeZone) — verified against Ramakrishnan's corrected file, valid for any person entered
--     with a real standard time zone (true for every row this app will ever produce; JH's own
--     bundled historical/LMT-ambiguous sample charts are a different, older file shape not
--     relevant here).
--   - Lines 8/11/12 are reserved/atlas-id fields with no equivalent in tbl_BirthDetails; 0 is
--     the safe default JH's own bundled files use for a manually-entered (non-atlas-lookup) place.
--
-- Table, not a view (rammyps's explicit choice, 2026-08-28): lets a computed value be manually
-- overridden per person (e.g. hand-tuning lat/long to match Jagannatha Hora's own atlas exactly)
-- without a view silently recomputing over it. IsOverridden marks a row usp_Chart_RecomputeJhdExport
-- must leave alone unless @Force = 1 is passed explicitly.
USE vedic_horo_gen;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_Chart_JhdExport')
BEGIN
    CREATE TABLE dbo.tbl_Chart_JhdExport
    (
        Id             INT IDENTITY(1,1) PRIMARY KEY,
        BirthDetailId  INT             NOT NULL UNIQUE REFERENCES dbo.tbl_BirthDetails(Id),

        -- one column per .jhd line, in file order
        Month          TINYINT         NOT NULL,   -- line 1
        Day            TINYINT         NOT NULL,   -- line 2
        Year           SMALLINT        NOT NULL,   -- line 3
        Time           DECIMAL(9,6)    NOT NULL,   -- line 4  HH.MM digit-literal
        TimeZone       DECIMAL(9,6)    NOT NULL,   -- line 5  -HH.MM digit-literal, negative = East
        Longitude      DECIMAL(9,6)    NOT NULL,   -- line 6  -DDD.MM digit-literal, negative = East
        Latitude       DECIMAL(9,6)    NOT NULL,   -- line 7  DD.MM digit-literal, positive = North
        Reserved1      DECIMAL(9,6)    NOT NULL DEFAULT 0,   -- line 8
        LmtRef1        DECIMAL(9,6)    NOT NULL,   -- line 9
        LmtRef2         DECIMAL(9,6)    NOT NULL,   -- line 10
        Reserved2      INT             NOT NULL DEFAULT 0,   -- line 11
        AtlasId        INT             NOT NULL DEFAULT 0,   -- line 12
        PlaceCity      NVARCHAR(200)   NOT NULL,   -- line 13
        PlaceCountry   NVARCHAR(200)   NOT NULL,   -- line 14
        Flag           TINYINT         NOT NULL DEFAULT 1,   -- line 15

        IsOverridden   BIT             NOT NULL DEFAULT 0,
        ComputedAt     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );

    CREATE UNIQUE INDEX IX_Chart_JhdExport_BirthDetailId ON dbo.tbl_Chart_JhdExport (BirthDetailId);
END
GO

-- Recomputes/upserts the .jhd field values for one person (@BirthDetailId) or everyone
-- (@BirthDetailId IS NULL). Rows marked IsOverridden = 1 are left untouched unless @Force = 1.
CREATE OR ALTER PROCEDURE dbo.usp_Chart_RecomputeJhdExport
    @BirthDetailId INT = NULL,
    @Force         BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Src AS (
        SELECT
            bd.Id                                              AS BirthDetailId,
            bd.DateOfBirth,
            COALESCE(bd.CorrectedTimeOfBirth, bd.TimeOfBirth)  AS EffTime,
            bd.Latitude,
            bd.Longitude,
            bd.UtcOffset,
            bd.PlaceCity,
            bd.PlaceCountry
        FROM dbo.tbl_BirthDetails bd
        WHERE (@BirthDetailId IS NULL OR bd.Id = @BirthDetailId)
    ),
    OffsetParsed AS (
        SELECT
            *,
            CASE WHEN LEFT(UtcOffset, 1) = '-' THEN -1 ELSE 1 END                                   AS OffsetSign,
            CAST(LEFT(REPLACE(UtcOffset, '-', ''), CHARINDEX(':', REPLACE(UtcOffset, '-', '')) - 1) AS INT) AS OffsetHour,
            CAST(SUBSTRING(REPLACE(UtcOffset, '-', ''), CHARINDEX(':', REPLACE(UtcOffset, '-', '')) + 1, 2) AS INT) AS OffsetMinute
        FROM Src
    ),
    Calc AS (
        SELECT
            BirthDetailId,
            CAST(MONTH(DateOfBirth) AS TINYINT)  AS Month,
            CAST(DAY(DateOfBirth)   AS TINYINT)  AS Day,
            CAST(YEAR(DateOfBirth)  AS SMALLINT) AS Year,
            CAST(DATEPART(HOUR, EffTime) + DATEPART(MINUTE, EffTime) / 100.0 AS DECIMAL(9,6)) AS Time,
            CAST(-1 * OffsetSign * (OffsetHour + OffsetMinute / 100.0) AS DECIMAL(9,6))       AS TimeZone,
            CAST(
                CASE WHEN Longitude >= 0 THEN -1 ELSE 1 END *
                (FLOOR(ABS(Longitude)) + ((ABS(Longitude) - FLOOR(ABS(Longitude))) * 60) / 100.0)
                AS DECIMAL(9,6)
            ) AS Longitude,
            CAST(
                SIGN(Latitude) *
                (FLOOR(ABS(Latitude)) + ((ABS(Latitude) - FLOOR(ABS(Latitude))) * 60) / 100.0)
                AS DECIMAL(9,6)
            ) AS Latitude,
            PlaceCity,
            PlaceCountry
        FROM OffsetParsed
    )
    MERGE dbo.tbl_Chart_JhdExport AS tgt
    USING Calc AS src
        ON tgt.BirthDetailId = src.BirthDetailId
    WHEN MATCHED AND (tgt.IsOverridden = 0 OR @Force = 1) THEN
        UPDATE SET
            Month = src.Month, Day = src.Day, Year = src.Year,
            Time = src.Time, TimeZone = src.TimeZone,
            Longitude = src.Longitude, Latitude = src.Latitude,
            LmtRef1 = src.TimeZone, LmtRef2 = src.TimeZone,
            PlaceCity = src.PlaceCity, PlaceCountry = src.PlaceCountry,
            ComputedAt = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (BirthDetailId, Month, Day, Year, Time, TimeZone, Longitude, Latitude,
                LmtRef1, LmtRef2, PlaceCity, PlaceCountry)
        VALUES (src.BirthDetailId, src.Month, src.Day, src.Year, src.Time, src.TimeZone,
                src.Longitude, src.Latitude, src.TimeZone, src.TimeZone,
                src.PlaceCity, src.PlaceCountry);
END
GO
