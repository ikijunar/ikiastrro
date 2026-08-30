-- 022_add_nakshatra_ruling_planet_lord_columns.sql
-- Adds the Nakshatra's ruling planet lord directly onto tbl_NakshatraPadas and
-- tbl_NakshatraSubLords, so it's selectable without a join to tbl_Nakshatras.
--
-- tbl_Nakshatras already has this (RulingPlanetId, migration 021) -- nothing to add there.
--
-- tbl_NakshatraSubLords already has SubLordId (the KP sub-period lord, 1-of-9-planets, different
-- from the Nakshatra's own lord) -- the new RulingPlanetId column is the *parent Nakshatra's*
-- lord, so a KP query can see both "Nakshatra Lord" and "Sub Lord" on the same row without a join.
--
-- Implemented as a COMPUTED column (via fn_GetNakshatraRulingPlanetId), not a physically stored/
-- duplicated value -- this is the same data as tbl_Nakshatras.RulingPlanetId, and storing a second
-- physical copy would risk drifting out of sync if tbl_Nakshatras is ever corrected. A non-
-- persisted computed column reads live off the parent table on every query: same convenience as a
-- stored column (selectable directly, no JOIN needed in the query text), zero sync risk.

IF OBJECT_ID('dbo.fn_GetNakshatraRulingPlanetId', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_GetNakshatraRulingPlanetId;
GO

CREATE FUNCTION dbo.fn_GetNakshatraRulingPlanetId (@NakshatraId TINYINT)
RETURNS TINYINT
AS
BEGIN
    DECLARE @PlanetId TINYINT;
    SELECT @PlanetId = RulingPlanetId FROM dbo.tbl_Nakshatras WHERE Id = @NakshatraId;
    RETURN @PlanetId;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_NakshatraPadas') AND name = 'RulingPlanetId')
BEGIN
    ALTER TABLE tbl_NakshatraPadas
        ADD RulingPlanetId AS (dbo.fn_GetNakshatraRulingPlanetId(NakshatraId));
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('tbl_NakshatraSubLords') AND name = 'RulingPlanetId')
BEGIN
    ALTER TABLE tbl_NakshatraSubLords
        ADD RulingPlanetId AS (dbo.fn_GetNakshatraRulingPlanetId(NakshatraId));
END
GO
