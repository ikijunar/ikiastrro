-- 026_create_combustion_orb_rules.sql
-- Transcribes ClassicalCombustion.DirectOrbDegrees + RetrogradeOrbDegrees verbatim.
-- RetrogradeOrbDegrees is NULL for planets with no documented retrograde-specific orb
-- (Moon, Jupiter, Saturn) -- those fall back to DirectOrbDegrees regardless of motion, exactly
-- as the current C# does (RetrogradeOrbDegrees.TryGetValue returning false).

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Rule_CombustionOrb')
BEGIN
    CREATE TABLE tbl_Rule_CombustionOrb
    (
        Id                    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RuleSetId             TINYINT      NOT NULL REFERENCES tbl_Rule_Sets(Id),
        PlanetId              TINYINT      NOT NULL REFERENCES tbl_Planets(Id),
        DirectOrbDegrees      DECIMAL(5,2) NOT NULL,
        RetrogradeOrbDegrees  DECIMAL(5,2) NULL,
        CONSTRAINT UQ_RuleCombustionOrb UNIQUE (RuleSetId, PlanetId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_Rule_CombustionOrb)
BEGIN
    -- PlanetId: 2 Moon,3 Mars,4 Mercury,5 Jupiter,6 Venus,7 Saturn
    INSERT INTO tbl_Rule_CombustionOrb (RuleSetId, PlanetId, DirectOrbDegrees, RetrogradeOrbDegrees)
    VALUES
        (1, 2, 12.00, NULL),   -- Moon
        (1, 3, 17.00, 8.00),   -- Mars
        (1, 4, 14.00, 12.00),  -- Mercury
        (1, 5, 11.00, NULL),   -- Jupiter
        (1, 6, 10.00, 8.00),   -- Venus
        (1, 7, 15.00, NULL);   -- Saturn
END
GO
