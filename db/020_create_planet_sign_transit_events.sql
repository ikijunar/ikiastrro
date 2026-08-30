-- 020_create_planet_sign_transit_events.sql
-- Point 2 of D:\@ClaudeSpace\ikiastrro\docs\vedic-reference-tables.md: sign-boundary-crossing
-- events for slow planets (Saturn, Jupiter, Rahu -- Mars deliberately excluded, per the
-- original scope; Ketu is derived from Rahu, never stored).
--
-- TABLE STRUCTURE ONLY -- no rows seeded by this migration. The 1930-2060 backfill requires
-- walking SwissEphemerisProvider day-by-day (with bisection refinement at each sign change),
-- which is a CLI feature (new backfill mode alongside backfill-analytics/backfill-dasha), not
-- something a SQL migration can compute. That CLI work is the next implementation step after
-- this table exists.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_PlanetSignTransitEvents')
BEGIN
    CREATE TABLE tbl_PlanetSignTransitEvents
    (
        Id                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PlanetId          TINYINT       NOT NULL REFERENCES tbl_Planets(Id),
        EventDateTimeUtc  DATETIME2(0)  NOT NULL,
        SignId            TINYINT       NOT NULL REFERENCES tbl_SignAttributes(Id),
        MotionDirection   VARCHAR(10)   NOT NULL CHECK (MotionDirection IN ('Direct','Retrograde')),
        IsReentry         BIT           NOT NULL DEFAULT 0,
        Notes             VARCHAR(100)  NULL,

        -- Saturn(7)/Jupiter(5)/Rahu(8) only -- Ketu is always derived from Rahu, never stored;
        -- Mars(3) intentionally excluded per the original point-2 scope (row-count trade-off
        -- noted in the scope doc -- add later if wanted, it's an additive change).
        CONSTRAINT CK_PlanetSignTransitEvents_PlanetId CHECK (PlanetId IN (5, 7, 8))
    );

    CREATE INDEX IX_PlanetSignTransitEvents_PlanetId_EventDateTimeUtc
        ON tbl_PlanetSignTransitEvents (PlanetId, EventDateTimeUtc);
END
GO

-- Ketu's transits are always Rahu's SignId + 6 signs (+180 degrees), never independently
-- computed or stored -- matches this project's existing Rahu/Ketu convention.
IF NOT EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_KetuSignTransitEvents')
    EXEC('
    CREATE VIEW vw_KetuSignTransitEvents AS
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
    ');
GO

-- "What sign was <planet> in on <date>" -- the actual query shape most consumers need, rather
-- than scanning raw events. Matches the tvf_Chart_LifeWeeks pattern already used in this project.
-- @PlanetId = 9 (Ketu) is handled transparently via Rahu's events + the same +6-sign derivation
-- as vw_KetuSignTransitEvents, so callers never need to special-case Ketu themselves.
IF OBJECT_ID('tvf_PlanetSignAtDate', 'IF') IS NOT NULL
    DROP FUNCTION tvf_PlanetSignAtDate;
GO

CREATE FUNCTION tvf_PlanetSignAtDate (@PlanetId TINYINT, @AsOfDateUtc DATETIME2(0))
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
GO
