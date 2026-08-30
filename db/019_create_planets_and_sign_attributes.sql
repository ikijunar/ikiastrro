-- 019_create_planets_and_sign_attributes.sql
-- Reference/master data: tbl_Planets (9-row planet master) + tbl_SignAttributes (12 Rashi
-- reference rows). Points 1/1a/1b of D:\@ClaudeSpace\cproj_vedic_horo_gen\docs\vedic-reference-tables.md.
--
-- Values cross-checked live against this project's own source of truth:
--   - ZodiacName enum (VedicHoroGen.Core\Astro\ZodiacName.cs) -- ZodiacEnumValue column below
--     exists specifically because that enum spells Capricorn as "Capricornus".
--   - AstroMath.NakshatraLordOrder / VimshottariDashaCalculator.YearsByLord -- VimshottariYears
--     and VimshottariSequenceOrder below match those exactly (Ketu 7, Venus 20, Sun 6, Moon 10,
--     Mars 7, Rahu 18, Jupiter 16, Saturn 19, Mercury 17 = 120 total).
--   - ClassicalDignity.cs -- OwnSigns/ExaltationSign/DebilitationSign/Moolatrikona tables.
--     NOTE: ClassicalDignity.cs stores exaltation/debilitation as SIGN ONLY, no exact degree --
--     ExaltedDegree/DebilitatedDegree below are supplementary classical reference values (BPHS),
--     NOT read by the current engine. Flagged so nobody assumes a degree-level dignity check
--     exists in code today.
--
-- RisingType is intentionally left NULL for all 12 rows -- classical texts disagree on the
-- exact sign-by-sign assignment (same discipline as the Rahu/Ketu dignity convention call).
-- Needs a specific cited source before populating, per rammyps's review.
--
-- SymbolAnimalType: Sagittarius is classically dual-natured (human archer torso + horse body)
-- and Capricorn dual-natured (goat foreparts + fish tail) -- simplified to a single value each
-- (Dwipada, Jalachara) here rather than modeling the split; flag if that's not granular enough.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Planets')
BEGIN
    CREATE TABLE tbl_Planets
    (
        Id                      TINYINT      NOT NULL PRIMARY KEY,
        PlanetName              VARCHAR(15)  NOT NULL UNIQUE,
        PlanetNameSanskrit      VARCHAR(15)  NOT NULL,
        NaturalNature           VARCHAR(12)  NOT NULL CHECK (NaturalNature IN ('Benefic','Malefic','Conditional')),
        ConditionalRule         VARCHAR(100) NULL,
        RulesSign               BIT          NOT NULL,   -- 1 = one of the 7 classical grahas that rules a sign; 0 = Rahu/Ketu
        VimshottariYears        TINYINT      NOT NULL,   -- must match VimshottariDashaCalculator.YearsByLord exactly
        VimshottariSequenceOrder TINYINT     NOT NULL UNIQUE  -- 1-9, position in the fixed Ketu->Mercury cycle (AstroMath.NakshatraLordOrder)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_Planets)
BEGIN
    INSERT INTO tbl_Planets (Id, PlanetName, PlanetNameSanskrit, NaturalNature, ConditionalRule, RulesSign, VimshottariYears, VimshottariSequenceOrder)
    VALUES
        (1, 'Sun',     'Surya',    'Malefic',     NULL,                                             1, 6,  3),
        (2, 'Moon',    'Chandra',  'Conditional', 'Benefic when waxing (Shukla Paksha)',             1, 10, 4),
        (3, 'Mars',    'Mangala',  'Malefic',     NULL,                                             1, 7,  5),
        (4, 'Mercury', 'Budha',    'Conditional', 'Benefic when unafflicted / conjunct benefics',    1, 17, 9),
        (5, 'Jupiter', 'Guru',     'Benefic',     NULL,                                             1, 16, 7),
        (6, 'Venus',   'Shukra',   'Benefic',     NULL,                                             1, 20, 2),
        (7, 'Saturn',  'Shani',    'Malefic',     NULL,                                             1, 19, 8),
        (8, 'Rahu',    'Rahu',     'Malefic',     NULL,                                             0, 18, 6),
        (9, 'Ketu',    'Ketu',     'Malefic',     NULL,                                             0, 7,  1);
END
GO

-- Sanity check: the 9 VimshottariYears must total exactly 120 (full Vimshottari cycle)
IF (SELECT SUM(VimshottariYears) FROM tbl_Planets) <> 120
BEGIN
    RAISERROR('tbl_Planets.VimshottariYears does not sum to 120 -- seed data error, do not proceed.', 16, 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_SignAttributes')
BEGIN
    CREATE TABLE tbl_SignAttributes
    (
        Id                      TINYINT       NOT NULL PRIMARY KEY,
        SignName                VARCHAR(20)   NOT NULL UNIQUE,
        SignNameSanskrit        VARCHAR(20)   NOT NULL,
        ZodiacEnumValue         VARCHAR(20)   NOT NULL UNIQUE,  -- exact ZodiacName enum spelling (Capricornus, not Capricorn)
        RulingPlanetId          TINYINT       NOT NULL REFERENCES tbl_Planets(Id),
        type_house_element      VARCHAR(10)   NOT NULL CHECK (type_house_element IN ('Fire','Earth','Air','Water')),
        type_house_keyattri     VARCHAR(15)   NOT NULL CHECK (type_house_keyattri IN ('Chara','Sthira','Dwiswabhava')),
        Gender                  VARCHAR(10)   NOT NULL CHECK (Gender IN ('Male','Female')),
        Direction               VARCHAR(10)   NOT NULL CHECK (Direction IN ('East','South','West','North')),
        RisingType               VARCHAR(15)   NULL CHECK (RisingType IN ('Sirshodaya','Prishthodaya','Ubhayodaya') OR RisingType IS NULL),
        SymbolAnimalType         VARCHAR(20)   NOT NULL CHECK (SymbolAnimalType IN ('Chatushpada','Dwipada','Keeta','Jalachara')),
        SymbolDescription        VARCHAR(50)   NOT NULL,
        KalapurushaBodyPart      VARCHAR(30)   NOT NULL,
        ExaltedPlanetId          TINYINT       NULL REFERENCES tbl_Planets(Id),
        ExaltedDegree            DECIMAL(5,2)  NULL,  -- classical reference (BPHS) -- NOT read by ClassicalDignity.cs today
        DebilitatedPlanetId      TINYINT       NULL REFERENCES tbl_Planets(Id),
        DebilitatedDegree        DECIMAL(5,2)  NULL,  -- classical reference (BPHS) -- NOT read by ClassicalDignity.cs today
        MooltrikonaPlanetId      TINYINT       NULL REFERENCES tbl_Planets(Id),
        MooltrikonaRangeStart    DECIMAL(5,2)  NULL,
        MooltrikonaRangeEnd      DECIMAL(5,2)  NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM tbl_SignAttributes)
BEGIN
    -- RulingPlanetId / ExaltedPlanetId / DebilitatedPlanetId / MooltrikonaPlanetId reference
    -- tbl_Planets.Id: 1 Sun, 2 Moon, 3 Mars, 4 Mercury, 5 Jupiter, 6 Venus, 7 Saturn, 8 Rahu, 9 Ketu
    INSERT INTO tbl_SignAttributes
        (Id, SignName, SignNameSanskrit, ZodiacEnumValue, RulingPlanetId, type_house_element, type_house_keyattri,
         Gender, Direction, RisingType, SymbolAnimalType, SymbolDescription, KalapurushaBodyPart,
         ExaltedPlanetId, ExaltedDegree, DebilitatedPlanetId, DebilitatedDegree,
         MooltrikonaPlanetId, MooltrikonaRangeStart, MooltrikonaRangeEnd)
    VALUES
        (1,  'Aries',       'Mesha',      'Aries',        3, 'Fire',  'Chara',       'Male',   'East',  NULL, 'Chatushpada', 'Ram',            'Head',            1, 10.0, 7, 20.0, 3, 0.0,  12.0),
        (2,  'Taurus',      'Vrishabha',  'Taurus',       6, 'Earth', 'Sthira',      'Female', 'South', NULL, 'Chatushpada', 'Bull',           'Face',            2, 3.0,  NULL, NULL, NULL, NULL, NULL),
        (3,  'Gemini',      'Mithuna',    'Gemini',       4, 'Air',   'Dwiswabhava', 'Male',   'West',  NULL, 'Dwipada',     'Twins',          'Arms/Shoulders',  NULL, NULL, NULL, NULL, NULL, NULL, NULL),
        (4,  'Cancer',      'Karka',      'Cancer',       2, 'Water', 'Chara',       'Female', 'North', NULL, 'Jalachara',   'Crab',           'Chest',           5, 5.0,  3, 28.0, NULL, NULL, NULL),
        (5,  'Leo',         'Simha',      'Leo',          1, 'Fire',  'Sthira',      'Male',   'East',  NULL, 'Chatushpada', 'Lion',           'Heart',           NULL, NULL, NULL, NULL, 1, 0.0, 20.0),
        (6,  'Virgo',       'Kanya',      'Virgo',        4, 'Earth', 'Dwiswabhava', 'Female', 'South', NULL, 'Dwipada',     'Virgin',         'Stomach',         4, 15.0, 6, 27.0, 4, 16.0, 20.0),
        (7,  'Libra',       'Tula',       'Libra',        6, 'Air',   'Chara',       'Male',   'West',  NULL, 'Dwipada',     'Scales',         'Navel/Pelvis',    7, 20.0, 1, 10.0, 6, 0.0, 15.0),
        (8,  'Scorpio',     'Vrishchika', 'Scorpio',      3, 'Water', 'Sthira',      'Female', 'North', NULL, 'Keeta',       'Scorpion',       'Genitals',        NULL, NULL, 2, 3.0, NULL, NULL, NULL),
        (9,  'Sagittarius', 'Dhanu',      'Sagittarius',  5, 'Fire',  'Dwiswabhava', 'Male',   'East',  NULL, 'Dwipada',     'Archer/Centaur', 'Thighs',          NULL, NULL, NULL, NULL, 5, 0.0, 10.0),
        (10, 'Capricorn',   'Makara',     'Capricornus',  7, 'Earth', 'Chara',       'Female', 'South', NULL, 'Jalachara',   'Sea-goat',       'Knees',           3, 28.0, 5, 5.0, NULL, NULL, NULL),
        (11, 'Aquarius',    'Kumbha',     'Aquarius',     7, 'Air',   'Sthira',      'Male',   'West',  NULL, 'Dwipada',     'Water-bearer',   'Calves/Ankles',   NULL, NULL, NULL, NULL, 7, 0.0, 20.0),
        (12, 'Pisces',      'Meena',      'Pisces',       5, 'Water', 'Dwiswabhava', 'Female', 'North', NULL, 'Jalachara',   'Fish',           'Feet',            6, 27.0, 4, 15.0, NULL, NULL, NULL);
END
GO
