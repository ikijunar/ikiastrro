-- 027_create_natural_relationship_rules.sql
-- Transcribes ClassicalDignity.NaturalRelationship verbatim -- 7x6=42 directed pairs (the 7
-- classical grahas only; Rahu/Ketu are explicitly excluded from this table in the C# source,
-- "not part of the Naisargika Maitri table in standard Parashari texts"). Deliberately NOT
-- assumed symmetric -- each pair transcribed from its own source line, not inferred from its
-- reverse.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Rule_NaturalRelationship')
BEGIN
    CREATE TABLE tbl_Rule_NaturalRelationship
    (
        Id                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RuleSetId         TINYINT     NOT NULL REFERENCES tbl_Rule_Sets(Id),
        PlanetId          TINYINT     NOT NULL REFERENCES tbl_Planets(Id),
        RelatedPlanetId   TINYINT     NOT NULL REFERENCES tbl_Planets(Id),
        RelationshipType  VARCHAR(10) NOT NULL CHECK (RelationshipType IN ('Friend','Neutral','Enemy')),
        CONSTRAINT UQ_RuleNaturalRelationship UNIQUE (RuleSetId, PlanetId, RelatedPlanetId),
        CONSTRAINT CK_RuleNaturalRelationship_NotSelf CHECK (PlanetId <> RelatedPlanetId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_Rule_NaturalRelationship)
BEGIN
    -- PlanetId: 1 Sun,2 Moon,3 Mars,4 Mercury,5 Jupiter,6 Venus,7 Saturn
    INSERT INTO tbl_Rule_NaturalRelationship (RuleSetId, PlanetId, RelatedPlanetId, RelationshipType)
    VALUES
    -- Sun: Friends Moon,Mars,Jupiter; Neutral Mercury; Enemies Venus,Saturn
    (1,1,2,'Friend'), (1,1,3,'Friend'), (1,1,5,'Friend'), (1,1,4,'Neutral'), (1,1,6,'Enemy'), (1,1,7,'Enemy'),
    -- Moon: Friends Sun,Mercury; Neutral Mars,Jupiter,Venus,Saturn; no enemies
    (1,2,1,'Friend'), (1,2,4,'Friend'), (1,2,3,'Neutral'), (1,2,5,'Neutral'), (1,2,6,'Neutral'), (1,2,7,'Neutral'),
    -- Mars: Friends Sun,Moon,Jupiter; Neutral Venus,Saturn; Enemy Mercury
    (1,3,1,'Friend'), (1,3,2,'Friend'), (1,3,5,'Friend'), (1,3,6,'Neutral'), (1,3,7,'Neutral'), (1,3,4,'Enemy'),
    -- Mercury: Friends Sun,Venus; Neutral Mars,Jupiter,Saturn; Enemy Moon
    (1,4,1,'Friend'), (1,4,6,'Friend'), (1,4,3,'Neutral'), (1,4,5,'Neutral'), (1,4,7,'Neutral'), (1,4,2,'Enemy'),
    -- Jupiter: Friends Sun,Moon,Mars; Neutral Saturn; Enemies Mercury,Venus
    (1,5,1,'Friend'), (1,5,2,'Friend'), (1,5,3,'Friend'), (1,5,7,'Neutral'), (1,5,4,'Enemy'), (1,5,6,'Enemy'),
    -- Venus: Friends Mercury,Saturn; Neutral Mars,Jupiter; Enemies Sun,Moon
    (1,6,4,'Friend'), (1,6,7,'Friend'), (1,6,3,'Neutral'), (1,6,5,'Neutral'), (1,6,1,'Enemy'), (1,6,2,'Enemy'),
    -- Saturn: Friends Mercury,Venus; Neutral Jupiter; Enemies Sun,Moon,Mars
    (1,7,4,'Friend'), (1,7,6,'Friend'), (1,7,5,'Neutral'), (1,7,1,'Enemy'), (1,7,2,'Enemy'), (1,7,3,'Enemy');
END
GO
