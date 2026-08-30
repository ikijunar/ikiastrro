-- 025_create_aspect_offset_rules.sql
-- Transcribes ClassicalRelationships.AspectOffsets verbatim -- every classical graha aspects
-- its own 7th; Mars/Jupiter/Saturn add their specials; Rahu/Ketu use the Jupiter-style
-- 5th/7th/9th convention per rammyps's 2026-08-24 decision.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Rule_AspectOffset')
BEGIN
    CREATE TABLE tbl_Rule_AspectOffset
    (
        Id           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RuleSetId    TINYINT      NOT NULL REFERENCES tbl_Rule_Sets(Id),
        PlanetId     TINYINT      NOT NULL REFERENCES tbl_Planets(Id),
        HouseOffset  TINYINT      NOT NULL CHECK (HouseOffset BETWEEN 1 AND 12),
        OffsetLabel  VARCHAR(10)  NOT NULL,
        CONSTRAINT UQ_RuleAspectOffset UNIQUE (RuleSetId, PlanetId, HouseOffset)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_Rule_AspectOffset)
BEGIN
    -- PlanetId: 1 Sun,2 Moon,3 Mars,4 Mercury,5 Jupiter,6 Venus,7 Saturn,8 Rahu,9 Ketu
    INSERT INTO tbl_Rule_AspectOffset (RuleSetId, PlanetId, HouseOffset, OffsetLabel)
    VALUES
        (1, 1, 7, '7th'),                                   -- Sun
        (1, 2, 7, '7th'),                                   -- Moon
        (1, 3, 4, '4th'), (1, 3, 7, '7th'), (1, 3, 8, '8th'),   -- Mars
        (1, 4, 7, '7th'),                                   -- Mercury
        (1, 5, 5, '5th'), (1, 5, 7, '7th'), (1, 5, 9, '9th'),   -- Jupiter
        (1, 6, 7, '7th'),                                   -- Venus
        (1, 7, 3, '3rd'), (1, 7, 7, '7th'), (1, 7, 10, '10th'), -- Saturn
        (1, 8, 5, '5th'), (1, 8, 7, '7th'), (1, 8, 9, '9th'),   -- Rahu
        (1, 9, 5, '5th'), (1, 9, 7, '7th'), (1, 9, 9, '9th');   -- Ketu
END
GO
