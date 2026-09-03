-- =====================================================================
-- 26 - Graha characters (BPHS ch. 3, PVR-consolidated) as normalized data.
--
-- Source: rammyps's PVR-consolidated graha-attributes worksheet
-- (docs/research/graha-characters-pvr.md). SourceRefCode = SRC_PVR_INTEGRATED
-- on every row. RuleSetId 1 (a BPHS variant would be a second rule-set).
--
--   tbl_Dim_GrahaAttribute   - the 16-row attribute catalog.
--   tbl_Rule_GrahaAttribute  - one row per (RuleSetId, GrahaId, AttributeCode);
--                              ValueCode (canonical token) + ValueText (label).
--                              A blank worksheet cell => NO row (means "not
--                              defined for this source", not "not implemented").
--   tbl_Rule_DigBala         - directional-strength reference house per graha
--                              (Lagna = 1). A STRENGTH input, kept separate
--                              from the generic attribute bag.
--
-- tbl_Planets (the graha master) is unchanged.
--
-- Idempotent: table adds guarded; seeds are IF NOT EXISTS on their table.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/26_add_rule_graha_attributes.sql
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- --- Batch 1: tbl_Dim_GrahaAttribute ---
IF OBJECT_ID('dbo.tbl_Dim_GrahaAttribute', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_GrahaAttribute (
        AttributeCode VARCHAR(30)  NOT NULL CONSTRAINT PK_Dim_GrahaAttribute PRIMARY KEY,
        DisplayName   NVARCHAR(80)  NOT NULL,
        ValueKind     VARCHAR(10)   NOT NULL CONSTRAINT CK_Dim_GrahaAttribute_Kind CHECK (ValueKind IN ('CODE','TEXT','NUMBER')),
        SortOrder     TINYINT       NOT NULL,
        Notes         NVARCHAR(400) NULL
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_GrahaAttribute)
    INSERT dbo.tbl_Dim_GrahaAttribute (AttributeCode, DisplayName, ValueKind, SortOrder, Notes)
    VALUES
    ('SUBSTANCE_CLASS',       N'Substance class (dhatu / mula / jiva)', 'CODE',  1, N'The class of substance the graha governs.'),
    ('BODY_DHATU',            N'Bodily dhatu (sapta-dhatu)',            'CODE',  2, N'The bodily tissue the graha signifies.'),
    ('TIME_PERIOD',           N'Time period lorded (kala)',             'CODE',  3, N'The unit of time the graha rules.'),
    ('DIURNAL_STRENGTH',      N'Diurnal strength',                     'CODE',  4, N'When the graha is strong: day, night, or always.'),
    ('RITU',                  N'Season lorded (ritu)',                  'CODE',  5, N'6-fold ritu lordship; the Sun lords the ayana, not a ritu.'),
    ('NATURAL_SIGNIFICATION', N'Primary natural signification',        'CODE',  6, N'The single core matter the graha governs.'),
    ('COLOR',                 N'Colour',                               'CODE',  7, NULL),
    ('ROYAL_STATUS',          N'Royal-cabinet rank',                   'CODE',  8, N'King / Prince / Minister / Army-chief / Servant / Soldier.'),
    ('PRESIDING_DEITY',       N'Presiding deity (adhidevata)',          'CODE',  9, NULL),
    ('GENDER',                N'Gender (linga)',                       'CODE', 10, N'PVR-consolidated worksheet uses Male / Female only.'),
    ('TATTVA',                N'Element (pancha-tattva)',               'CODE', 11, NULL),
    ('GENERAL_CHARACTER',     N'General disposition',                  'TEXT', 12, N'Free-text summary of the graha''s nature.'),
    ('VARNA',                 N'Varna (caste)',                        'CODE', 13, NULL),
    ('VARNA_TRAIT',           N'Trait implied by the varna',           'CODE', 14, N'1:1 function of VARNA; kept to preserve the worksheet 1:1.'),
    ('GUNA',                  N'Guna',                                 'CODE', 15, NULL),
    ('RESIDENCE',             N'Residence (vasa-sthana)',               'CODE', 16, NULL);
GO

-- --- Batch 2: tbl_Rule_GrahaAttribute ---
IF OBJECT_ID('dbo.tbl_Rule_GrahaAttribute', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_GrahaAttribute (
        Id            INT IDENTITY(1,1) NOT NULL
                          CONSTRAINT PK_Rule_GrahaAttribute PRIMARY KEY,
        RuleSetId     TINYINT      NOT NULL
                          CONSTRAINT FK_Rule_GrahaAttribute_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        GrahaId       TINYINT      NOT NULL
                          CONSTRAINT FK_Rule_GrahaAttribute_Planet FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        AttributeCode VARCHAR(30)  NOT NULL
                          CONSTRAINT FK_Rule_GrahaAttribute_Attr FOREIGN KEY REFERENCES dbo.tbl_Dim_GrahaAttribute (AttributeCode),
        ValueCode     VARCHAR(40)   NULL,       -- canonical token; NULL only for TEXT attributes
        ValueText     NVARCHAR(200) NOT NULL,   -- readable label (always present)
        Priority      TINYINT      NOT NULL CONSTRAINT DF_Rule_GrahaAttribute_Priority DEFAULT 1,
        SourceRefCode VARCHAR(40)   NULL,
        IsActive      BIT          NOT NULL CONSTRAINT DF_Rule_GrahaAttribute_IsActive DEFAULT 1,
        Notes         NVARCHAR(400) NULL,
        CONSTRAINT CK_RuleGrahaAttr_Src CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_GrahaAttribute UNIQUE (RuleSetId, GrahaId, AttributeCode)
    );
    CREATE NONCLUSTERED INDEX IX_Rule_GrahaAttribute_Attr
        ON dbo.tbl_Rule_GrahaAttribute (AttributeCode, GrahaId);
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_GrahaAttribute)
BEGIN
    ;WITH a (PlanetName, AttributeCode, ValueCode, ValueText, Notes) AS (
        SELECT * FROM (VALUES
        -- Sun
        ('Sun','SUBSTANCE_CLASS','VEGETABLE',N'Roots and vegetables',CONVERT(NVARCHAR(400),NULL)),
        ('Sun','BODY_DHATU','ASTHI',N'Bones',NULL),
        ('Sun','TIME_PERIOD','SIX_MONTHS',N'6 months',NULL),
        ('Sun','DIURNAL_STRENGTH','DAY',N'Day',NULL),
        ('Sun','NATURAL_SIGNIFICATION','SOUL',N'Soul',NULL),
        ('Sun','COLOR','BLOOD_RED',N'Blood red',NULL),
        ('Sun','ROYAL_STATUS','KING',N'King',NULL),
        ('Sun','PRESIDING_DEITY','AGNI',N'Agni (fire god)',NULL),
        ('Sun','GENDER','MALE',N'Male',NULL),
        ('Sun','TATTVA','AGNI',N'Agni (fire)',NULL),
        ('Sun','VARNA','KSHATRIYA',N'Kshatriya',NULL),
        ('Sun','VARNA_TRAIT','BRAVERY',N'Bravery',NULL),
        ('Sun','GUNA','SATTVA',N'Sattva (pure, truthful)',NULL),
        ('Sun','RESIDENCE','TEMPLE',N'Temple',NULL),
        -- Moon
        ('Moon','SUBSTANCE_CLASS','MINERAL',N'Metals and materials',NULL),
        ('Moon','BODY_DHATU','RAKTA',N'Blood',NULL),
        ('Moon','TIME_PERIOD','MINUTE',N'Minute',NULL),
        ('Moon','DIURNAL_STRENGTH','NIGHT',N'Night',NULL),
        ('Moon','RITU','RAINY',N'Rainy season',NULL),
        ('Moon','NATURAL_SIGNIFICATION','MIND',N'Mind',NULL),
        ('Moon','COLOR','TAWNY',N'Tawny',NULL),
        ('Moon','ROYAL_STATUS','KING',N'King',NULL),
        ('Moon','PRESIDING_DEITY','VARUNA',N'Varuna (rain god)',NULL),
        ('Moon','GENDER','FEMALE',N'Female',NULL),
        ('Moon','TATTVA','JALA',N'Jala (water)',NULL),
        ('Moon','VARNA','VAISHYA',N'Vaishya',NULL),
        ('Moon','VARNA_TRAIT','SOCIABILITY',N'Getting along with others',NULL),
        ('Moon','GUNA','SATTVA',N'Sattva (pure, truthful)',NULL),
        ('Moon','RESIDENCE','WATERY_PLACE',N'Watery place',NULL),
        -- Mars
        ('Mars','SUBSTANCE_CLASS','MINERAL',N'Metals and materials',NULL),
        ('Mars','BODY_DHATU','MAJJA',N'Marrow',NULL),
        ('Mars','TIME_PERIOD','WEEK',N'Week',NULL),
        ('Mars','DIURNAL_STRENGTH','NIGHT',N'Night',NULL),
        ('Mars','RITU','SUMMER',N'Summer',NULL),
        ('Mars','NATURAL_SIGNIFICATION','STRENGTH',N'Strength',NULL),
        ('Mars','COLOR','BLOOD_RED',N'Blood red',NULL),
        ('Mars','ROYAL_STATUS','ARMY_CHIEF',N'Army chief',NULL),
        ('Mars','PRESIDING_DEITY','SUBRAHMANYA',N'Subrahmanya (army-chief god)',NULL),
        ('Mars','GENDER','MALE',N'Male',NULL),
        ('Mars','TATTVA','AGNI',N'Agni (fire)',NULL),
        ('Mars','GENERAL_CHARACTER',NULL,N'Leadership, enterprise',NULL),
        ('Mars','VARNA','KSHATRIYA',N'Kshatriya',NULL),
        ('Mars','VARNA_TRAIT','BRAVERY',N'Bravery',NULL),
        ('Mars','GUNA','TAMAS',N'Tamas (dark, mean, depraved)',NULL),
        -- Mercury
        ('Mercury','SUBSTANCE_CLASS','ANIMAL',N'Living beings',NULL),
        ('Mercury','BODY_DHATU','TWAK',N'Skin',NULL),
        ('Mercury','TIME_PERIOD','TWO_MONTHS',N'2 months',NULL),
        ('Mercury','DIURNAL_STRENGTH','ALWAYS',N'Always',NULL),
        ('Mercury','RITU','DEW',N'Dew (autumn)',NULL),
        ('Mercury','NATURAL_SIGNIFICATION','SPEECH',N'Speech',NULL),
        ('Mercury','COLOR','GRASS_GREEN',N'Grass green',NULL),
        ('Mercury','ROYAL_STATUS','PRINCE',N'Prince',NULL),
        ('Mercury','PRESIDING_DEITY','MAHA_VISHNU',N'Maha Vishnu (supreme sustaining force)',NULL),
        ('Mercury','GENDER','FEMALE',N'Female',N'PVR-consolidated worksheet; classical BPHS ch. 3 assigns neuter to Mercury.'),
        ('Mercury','TATTVA','PRITHVI',N'Prithvi (earth)',NULL),
        ('Mercury','GENERAL_CHARACTER',NULL,N'Memory, logical abilities',NULL),
        ('Mercury','VARNA','VAISHYA',N'Vaishya',N'PVR-consolidated; several BPHS renderings give Shudra for Mercury.'),
        ('Mercury','VARNA_TRAIT','SOCIABILITY',N'Getting along with others',NULL),
        ('Mercury','GUNA','RAJAS',N'Rajas (passionate, energetic, impure)',NULL),
        ('Mercury','RESIDENCE','SPORTS_GROUND',N'Sports ground',NULL),
        -- Jupiter
        ('Jupiter','SUBSTANCE_CLASS','ANIMAL',N'Living beings',NULL),
        ('Jupiter','BODY_DHATU','MEDAS',N'Fat',NULL),
        ('Jupiter','TIME_PERIOD','MONTH',N'Month',NULL),
        ('Jupiter','DIURNAL_STRENGTH','DAY',N'Day',NULL),
        ('Jupiter','RITU','WINTER',N'Winter',NULL),
        ('Jupiter','NATURAL_SIGNIFICATION','KNOWLEDGE_HAPPINESS',N'Knowledge and happiness',NULL),
        ('Jupiter','COLOR','TAWNY',N'Tawny',NULL),
        ('Jupiter','ROYAL_STATUS','MINISTER',N'Minister',NULL),
        ('Jupiter','PRESIDING_DEITY','INDRA',N'Indra (ruler of the gods)',NULL),
        ('Jupiter','GENDER','MALE',N'Male',NULL),
        ('Jupiter','TATTVA','AKASHA',N'Akasha (ether)',NULL),
        ('Jupiter','GENERAL_CHARACTER',NULL,N'Wisdom, intelligence, perceiving knowledge',NULL),
        ('Jupiter','VARNA','BRAHMANA',N'Brahmana',NULL),
        ('Jupiter','VARNA_TRAIT','LEARNING',N'Learning and intelligence',NULL),
        ('Jupiter','GUNA','SATTVA',N'Sattva (pure, truthful)',NULL),
        ('Jupiter','RESIDENCE','TREASURE_HOUSE',N'Treasure house',NULL),
        -- Venus
        ('Venus','SUBSTANCE_CLASS','VEGETABLE',N'Roots and vegetables',NULL),
        ('Venus','BODY_DHATU','SHUKRA',N'Semen',NULL),
        ('Venus','TIME_PERIOD','FORTNIGHT',N'Fortnight',NULL),
        ('Venus','DIURNAL_STRENGTH','DAY',N'Day',NULL),
        ('Venus','RITU','SPRING',N'Spring',NULL),
        ('Venus','NATURAL_SIGNIFICATION','POTENCY',N'Potency',NULL),
        ('Venus','COLOR','VARIEGATED',N'Variegated',NULL),
        ('Venus','ROYAL_STATUS','MINISTER',N'Minister',NULL),
        ('Venus','PRESIDING_DEITY','SACHI',N'Sachi Devi (Indra''s consort)',NULL),
        ('Venus','GENDER','FEMALE',N'Female',NULL),
        ('Venus','TATTVA','JALA',N'Jala (water)',NULL),
        ('Venus','GENERAL_CHARACTER',NULL,N'Imagination, creative work',NULL),
        ('Venus','VARNA','BRAHMANA',N'Brahmana',NULL),
        ('Venus','VARNA_TRAIT','LEARNING',N'Learning and intelligence',NULL),
        ('Venus','GUNA','RAJAS',N'Rajas (passionate, energetic, impure)',NULL),
        -- Saturn
        ('Saturn','SUBSTANCE_CLASS','MINERAL',N'Metals and materials',NULL),
        ('Saturn','BODY_DHATU','SNAYU',N'Muscles',NULL),
        ('Saturn','TIME_PERIOD','YEAR',N'Year',NULL),
        ('Saturn','DIURNAL_STRENGTH','NIGHT',N'Night',NULL),
        ('Saturn','RITU','FALL',N'Fall (late winter)',NULL),
        ('Saturn','NATURAL_SIGNIFICATION','GRIEF',N'Grief',NULL),
        ('Saturn','COLOR','BLACK',N'Black',NULL),
        ('Saturn','ROYAL_STATUS','SERVANT',N'Servant',NULL),
        ('Saturn','PRESIDING_DEITY','BRAHMA',N'Brahma (the creator)',NULL),
        ('Saturn','GENDER','FEMALE',N'Female',N'PVR-consolidated worksheet; classical BPHS ch. 3 assigns neuter to Saturn.'),
        ('Saturn','TATTVA','VAYU',N'Vayu (air)',NULL),
        ('Saturn','GENERAL_CHARACTER',NULL,N'Wandering, free spirit',NULL),
        ('Saturn','VARNA','SHUDRA',N'Shudra',NULL),
        ('Saturn','VARNA_TRAIT','DILIGENCE',N'Hard working',NULL),
        ('Saturn','GUNA','TAMAS',N'Tamas (dark, mean, depraved)',NULL),
        ('Saturn','RESIDENCE','FILTHY_AREA',N'Filthy area',NULL),
        -- Rahu
        ('Rahu','SUBSTANCE_CLASS','MINERAL',N'Metals and materials',NULL),
        ('Rahu','ROYAL_STATUS','SOLDIER',N'Soldier',NULL),
        -- Ketu
        ('Ketu','SUBSTANCE_CLASS','ANIMAL',N'Living beings',NULL),
        ('Ketu','ROYAL_STATUS','SOLDIER',N'Soldier',NULL)
        ) v (PlanetName, AttributeCode, ValueCode, ValueText, Notes)
    )
    INSERT dbo.tbl_Rule_GrahaAttribute
        (RuleSetId, GrahaId, AttributeCode, ValueCode, ValueText, Priority, SourceRefCode, IsActive, Notes)
    SELECT 1, p.Id, a.AttributeCode, a.ValueCode, a.ValueText, 1, 'SRC_PVR_INTEGRATED', 1, a.Notes
    FROM a
    JOIN dbo.tbl_Planets p ON p.PlanetName = a.PlanetName;
END
GO

-- --- Batch 3: tbl_Rule_DigBala ---
IF OBJECT_ID('dbo.tbl_Rule_DigBala', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_DigBala (
        Id            INT IDENTITY(1,1) NOT NULL
                          CONSTRAINT PK_Rule_DigBala PRIMARY KEY,
        RuleSetId     TINYINT      NOT NULL
                          CONSTRAINT FK_Rule_DigBala_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        GrahaId       TINYINT      NOT NULL
                          CONSTRAINT FK_Rule_DigBala_Planet FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        DigBalaHouse  TINYINT      NOT NULL,
        MethodCode           VARCHAR(30)   NULL,
        RuleParametersJson   NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode        VARCHAR(40)   NULL,
        IsActive             BIT NOT NULL CONSTRAINT DF_Rule_DigBala_IsActive DEFAULT 1,
        CONSTRAINT CK_RuleDigBala_House CHECK (DigBalaHouse BETWEEN 1 AND 12),
        CONSTRAINT CK_RuleDigBala_Json  CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_RuleDigBala_Src   CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_DigBala UNIQUE (RuleSetId, GrahaId)
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_DigBala)
    INSERT dbo.tbl_Rule_DigBala (RuleSetId, GrahaId, DigBalaHouse, MethodCode, SourceRefCode)
    SELECT 1, p.Id, v.DigBalaHouse, 'HOUSE_LOOKUP', 'SRC_PVR_INTEGRATED'
    FROM (VALUES
        ('Sun',10),('Moon',4),('Mars',10),('Mercury',1),('Jupiter',1),('Venus',4),('Saturn',7)
    ) v (PlanetName, DigBalaHouse)
    JOIN dbo.tbl_Planets p ON p.PlanetName = v.PlanetName;
GO

-- --- Batch 4: tbl_Rule_Catalog rows ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_GrahaAttribute')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_GrahaAttribute', 'GRAHA', 'ATTR_LOOKUP',
            'Static graha characters (BPHS ch. 3, PVR-consolidated): substance class, body dhatu, kala unit, diurnal strength, ritu, natural signification, colour, royal rank, deity, gender, tattva, varna, guna, residence. One row per (rule-set, graha, attribute); blank source cell = no row.',
            '26_add_rule_graha_attributes.sql');
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_DigBala')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_DigBala', 'STRENGTH', 'HOUSE_LOOKUP',
            'Directional-strength (Dig Bala) reference house per graha - the bhava of full digbala (Lagna=1 / 4th / 7th / 10th). One strength input, not overall planetary strength.',
            '26_add_rule_graha_attributes.sql');
GO

-- --- Batch 5: ledger + summary ---
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '26_add_rule_graha_attributes.sql',
       'tbl_Dim_GrahaAttribute (16) + tbl_Rule_GrahaAttribute (BPHS ch.3, PVR-consolidated) + tbl_Rule_DigBala (7); 2 tbl_Rule_Catalog rows'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '26_add_rule_graha_attributes.sql');
GO

DECLARE @dim INT  = (SELECT COUNT(*) FROM dbo.tbl_Dim_GrahaAttribute);
DECLARE @attr INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaAttribute);
DECLARE @dig INT  = (SELECT COUNT(*) FROM dbo.tbl_Rule_DigBala);
DECLARE @orphan INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaAttribute r
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_GrahaAttribute d WHERE d.AttributeCode = r.AttributeCode));
DECLARE @codenull INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_GrahaAttribute r
    JOIN dbo.tbl_Dim_GrahaAttribute d ON d.AttributeCode = r.AttributeCode
    WHERE d.ValueKind = 'CODE' AND r.ValueCode IS NULL);
PRINT '26 applied: ' + CAST(@dim AS VARCHAR(10)) + ' dim rows (expect 16), '
    + CAST(@attr AS VARCHAR(10)) + ' attribute rows (expect 111), '
    + CAST(@dig AS VARCHAR(10)) + ' digbala rows (expect 7), '
    + CAST(@orphan AS VARCHAR(10)) + ' orphan attr codes (expect 0), '
    + CAST(@codenull AS VARCHAR(10)) + ' CODE rows missing ValueCode (expect 0).';
GO
