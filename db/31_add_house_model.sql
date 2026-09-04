-- =====================================================================
-- 31 - House model: bhava characteristics, significations, attributes.
--
-- PVR "Integrated Approach" Chapter 7. Mirrors the graha treatment
-- (migration 26: tbl_Dim_GrahaAttribute + tbl_Rule_GrahaAttribute) on
-- the house side, and fills the reserved tbl_Rule_HouseSignification.
--
--   tbl_Dim_House            - the 12-bhava master. 1:1, rule-set-invariant
--                              facts only: Sanskrit name, one-line short
--                              name, purushartha (sec 7.4.1), visible /
--                              invisible half (sec 7.4.5), Kala Purusha
--                              body part (sec 7.2), and the seven sec 7.4
--                              special-category memberships as bit flags
--                              (+ IsMaraka from the longevity chapter).
--   tbl_Dim_HouseCategory    - descriptive catalogue of the special
--                              categories (kendra / trikona / panaphara /
--                              apoklima / upachaya / dusthana / chaturasra
--                              + maraka), each with the sec 7.4.6 quick-
--                              summary effect and presiding deity.
--   tbl_Rule_HouseSignification - RESERVED (migration 18) -> POPULATED.
--                              ~110 bhava karakatvas from sec 7.2, one row
--                              per matter, RuleSetId 1, SRC_PVR_INTEGRATED.
--                              Gains SignificationText / SignificationCategory
--                              / DisplayOrder; RuleSetId tightened INT ->
--                              TINYINT + FK; FK on HouseNumber.
--   tbl_Dim_HouseAttribute   - attribute catalogue (mirror of
--                              tbl_Dim_GrahaAttribute): general character,
--                              sec 7.4.6 category effect, naisargika
--                              significator (the last seeded in migration 32
--                              from PVR Table 12).
--   tbl_Rule_HouseAttribute  - EAV values, one row per (rule-set, house,
--                              attribute, priority). Mirror of
--                              tbl_Rule_GrahaAttribute.
--   tbl_Astro_Terminology    += Category 'HouseCategory'; 8 HCAT_* + 4
--                              PURUSHARTHA_* + 2 ZHALF_* concepts, each with
--                              sa + en text (addendum, as migrations 29-30 -
--                              TerminologySeed.cs does not emit these yet).
--
-- Reference points (Chandra / Ravi / Paaka / Arudha / Karakamsa lagnas,
-- graha lagnas + Table 12, tbl_Fact_HouseFromReference) are migration 32.
--
-- Idempotent throughout. Apply:
--   sqlcmd -S localhost -E -d ikiastrro -b -i db/31_add_house_model.sql
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- --- Batch 1: tbl_Dim_House (12-bhava master) ---
IF OBJECT_ID('dbo.tbl_Dim_House', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_House (
        HouseNumber         TINYINT      NOT NULL
                                CONSTRAINT PK_Dim_House PRIMARY KEY,     -- fixed 1..12 domain; the natural key used across the schema
        BhavaNameSa         VARCHAR(20)  NOT NULL,                       -- Tanu, Dhana, Sahaja, ...
        ShortName           NVARCHAR(40) NOT NULL,
        PurusharthaCode     VARCHAR(8)   NOT NULL,                       -- PVR sec 7.4.1
        ZodiacHalf          VARCHAR(10)  NOT NULL,                       -- PVR sec 7.4.5
        KalapurushaBodyPart NVARCHAR(60) NOT NULL,                       -- PVR sec 7.2
        IsKendra            BIT          NOT NULL,                       -- PVR sec 7.4 memberships
        IsTrikona           BIT          NOT NULL,
        IsPanaphara         BIT          NOT NULL,
        IsApoklima          BIT          NOT NULL,
        IsUpachaya          BIT          NOT NULL,
        IsDusthana          BIT          NOT NULL,
        IsChaturasra        BIT          NOT NULL,
        IsMaraka            BIT          NOT NULL,                       -- longevity chapter, not sec 7.4
        IsActive            BIT          NOT NULL CONSTRAINT DF_Dim_House_IsActive DEFAULT 1,
        SourceRefCode       VARCHAR(40)  NULL,
        CONSTRAINT CK_Dim_House_Num         CHECK (HouseNumber BETWEEN 1 AND 12),
        CONSTRAINT CK_Dim_House_Purushartha CHECK (PurusharthaCode IN ('Dharma','Artha','Kama','Moksha')),
        CONSTRAINT CK_Dim_House_Half        CHECK (ZodiacHalf IN ('Visible','Invisible')),
        CONSTRAINT CK_Dim_House_Src         CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_House)
    INSERT dbo.tbl_Dim_House
        (HouseNumber, BhavaNameSa, ShortName, PurusharthaCode, ZodiacHalf, KalapurushaBodyPart,
         IsKendra, IsTrikona, IsPanaphara, IsApoklima, IsUpachaya, IsDusthana, IsChaturasra, IsMaraka, SourceRefCode)
    VALUES
    ( 1,'Tanu',   N'Self, body & vitality',              'Dharma','Invisible',N'Head',                          1,1,0,0,0,0,0,0,'SRC_PVR_INTEGRATED'),
    ( 2,'Dhana',  N'Wealth, family & speech',            'Artha', 'Invisible',N'Face, right eye, mouth',        0,0,1,0,0,0,0,1,'SRC_PVR_INTEGRATED'),
    ( 3,'Sahaja', N'Siblings, courage & communication',  'Kama',  'Invisible',N'Arms, shoulders, throat, ears', 0,0,0,1,1,0,0,0,'SRC_PVR_INTEGRATED'),
    ( 4,'Bandhu', N'Mother, home & happiness',           'Moksha','Invisible',N'Chest, heart',                  1,0,0,0,0,0,1,0,'SRC_PVR_INTEGRATED'),
    ( 5,'Putra',  N'Children, intellect & merit',        'Dharma','Invisible',N'Stomach, upper abdomen',        0,1,1,0,0,0,0,0,'SRC_PVR_INTEGRATED'),
    ( 6,'Ari',    N'Enemies, disease & service',         'Artha', 'Invisible',N'Lower abdomen, hips',           0,0,0,1,1,1,0,0,'SRC_PVR_INTEGRATED'),
    ( 7,'Yuvati', N'Spouse, marriage & partnership',     'Kama',  'Visible',  N'Below the navel, pelvis',       1,0,0,0,0,0,0,1,'SRC_PVR_INTEGRATED'),
    ( 8,'Randhra',N'Longevity, upheaval & the hidden',   'Moksha','Visible',  N'Genitals, excretory organs',    0,0,1,0,0,1,1,0,'SRC_PVR_INTEGRATED'),
    ( 9,'Dharma', N'Father, fortune & dharma',           'Dharma','Visible',  N'Thighs',                        0,1,0,1,0,0,0,0,'SRC_PVR_INTEGRATED'),
    (10,'Karma',  N'Career, action & status',            'Artha', 'Visible',  N'Knees',                         1,0,0,0,1,0,0,0,'SRC_PVR_INTEGRATED'),
    (11,'Labha',  N'Gains, income & elder siblings',     'Kama',  'Visible',  N'Calves, ankles',               0,0,1,0,1,0,0,0,'SRC_PVR_INTEGRATED'),
    (12,'Vyaya',  N'Loss, expense & liberation',         'Moksha','Visible',  N'Feet, left eye',               0,0,0,1,0,1,0,0,'SRC_PVR_INTEGRATED');
GO

-- --- Batch 2: tbl_Dim_HouseCategory (PVR sec 7.4 special categories) ---
IF OBJECT_ID('dbo.tbl_Dim_HouseCategory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_HouseCategory (
        CategoryCode    VARCHAR(16)   NOT NULL
                            CONSTRAINT PK_Dim_HouseCategory PRIMARY KEY,
        DisplayName     NVARCHAR(40)  NOT NULL,
        AltNamesEn      VARCHAR(80)   NULL,
        EffectSummary   NVARCHAR(200) NOT NULL,                          -- PVR sec 7.4.6
        PresidingDeity  VARCHAR(20)   NULL,
        HouseSet        VARCHAR(24)   NOT NULL,                          -- convenience: "1,4,7,10"
        IsClassicalOnly BIT           NOT NULL CONSTRAINT DF_Dim_HouseCategory_Classical DEFAULT 0,
        SortOrder       TINYINT       NOT NULL,
        SourceRefCode   VARCHAR(40)   NULL,
        CONSTRAINT CK_Dim_HouseCategory_Src CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_HouseCategory)
    INSERT dbo.tbl_Dim_HouseCategory
        (CategoryCode, DisplayName, AltNamesEn, EffectSummary, PresidingDeity, HouseSet, IsClassicalOnly, SortOrder, SourceRefCode)
    VALUES
    ('KENDRA',    N'Kendra',    'quadrant, angle, chatushtaya', N'Sustenance and vital activity; the pillars of the chart. Abode of Vishnu.',           'Vishnu',  '1,4,7,10', 0, 1, 'SRC_PVR_INTEGRATED'),
    ('TRIKONA',   N'Trikona',   'trine, kona',                  N'Prosperity and flourishing; dharma and grace. Abode of Lakshmi.',                    'Lakshmi', '1,5,9',    0, 2, 'SRC_PVR_INTEGRATED'),
    ('PANAPHARA', N'Panaphara', 'succedent',                    N'Quadrants counted from the 2nd; accumulation and holding.',                          NULL,      '2,5,8,11', 0, 3, 'SRC_PVR_INTEGRATED'),
    ('APOKLIMA',  N'Apoklima',  'cadent, precedent',            N'Quadrants counted from the 3rd; seeking, effort and dissipation.',                   NULL,      '3,6,9,12', 0, 4, 'SRC_PVR_INTEGRATED'),
    ('UPACHAYA',  N'Upachaya',  'house of growth',              N'Gains and growth; matters that improve with time and sustained effort.',             NULL,      '3,6,10,11',0, 5, 'SRC_PVR_INTEGRATED'),
    ('DUSTHANA',  N'Dusthana',  'trik sthana, evil house',      N'Setbacks and obstacles; suffering, loss and dissolution.',                           NULL,      '6,8,12',   0, 6, 'SRC_PVR_INTEGRATED'),
    ('CHATURASRA',N'Chaturasra','quadrangle',                   N'The 4th and 8th together; latent strain around home and longevity.',                 NULL,      '4,8',      0, 7, 'SRC_PVR_INTEGRATED'),
    ('MARAKA',    N'Maraka',    'killer house',                 N'Classical death-inflicting houses; from the longevity chapter, not PVR sec 7.4.',    NULL,      '2,7',      1, 8, 'SRC_PVR_INTEGRATED');
GO

-- --- Batch 3: tbl_Rule_HouseSignification - reserved -> populated ---
-- Tighten RuleSetId INT -> TINYINT and add the FK (table is empty).
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.tbl_Rule_HouseSignification')
             AND name = 'RuleSetId' AND system_type_id = TYPE_ID('int'))
    ALTER TABLE dbo.tbl_Rule_HouseSignification ALTER COLUMN RuleSetId TINYINT NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Rule_HouseSignification_RuleSet')
    ALTER TABLE dbo.tbl_Rule_HouseSignification
        ADD CONSTRAINT FK_Rule_HouseSignification_RuleSet FOREIGN KEY (RuleSetId) REFERENCES dbo.tbl_Rule_Sets (Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Rule_HouseSignification_House')
    ALTER TABLE dbo.tbl_Rule_HouseSignification
        ADD CONSTRAINT FK_Rule_HouseSignification_House FOREIGN KEY (HouseNumber) REFERENCES dbo.tbl_Dim_House (HouseNumber);
GO
IF COL_LENGTH('dbo.tbl_Rule_HouseSignification', 'SignificationText') IS NULL
    ALTER TABLE dbo.tbl_Rule_HouseSignification ADD SignificationText NVARCHAR(200) NULL;
GO
IF COL_LENGTH('dbo.tbl_Rule_HouseSignification', 'SignificationCategory') IS NULL
    ALTER TABLE dbo.tbl_Rule_HouseSignification ADD SignificationCategory VARCHAR(20) NULL
        CONSTRAINT CK_Rule_HouseSignification_Cat
        CHECK (SignificationCategory IS NULL OR SignificationCategory IN ('Matter','Person','BodyPart','DerivedHouse','Trait'));
GO
IF COL_LENGTH('dbo.tbl_Rule_HouseSignification', 'DisplayOrder') IS NULL
    ALTER TABLE dbo.tbl_Rule_HouseSignification ADD DisplayOrder TINYINT NOT NULL CONSTRAINT DF_Rule_HouseSignification_Order DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Rule_HouseSignification')
    CREATE UNIQUE INDEX UQ_Rule_HouseSignification
        ON dbo.tbl_Rule_HouseSignification (RuleSetId, HouseNumber, SignificationCode);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_HouseSignification)
BEGIN
    ;WITH s (HouseNumber, SignificationCode, SignificationText, SignificationCategory, DisplayOrder) AS (
        SELECT * FROM (VALUES
        -- 1st house (sec 7.2)
        ( 1,'H01_SELF',              N'The self and the "spirit of I"',                       'Matter',      1),
        ( 1,'H01_PHYSICAL_BODY',     N'Physical body and constitution',                      'Matter',      2),
        ( 1,'H01_APPEARANCE',        N'Appearance and complexion',                           'Matter',      3),
        ( 1,'H01_HEAD',              N'Head',                                                'BodyPart',    4),
        ( 1,'H01_INTELLIGENCE',      N'Intelligence',                                        'Matter',      5),
        ( 1,'H01_STRENGTH_ENERGY',   N'Strength, vigour and energy',                         'Matter',      6),
        ( 1,'H01_FAME',              N'Fame and reputation',                                 'Matter',      7),
        ( 1,'H01_SUCCESS',           N'Success in undertakings',                             'Matter',      8),
        ( 1,'H01_NATURE_OF_BIRTH',   N'Nature and circumstances of birth',                   'Matter',      9),
        ( 1,'H01_CASTE',             N'Caste',                                               'Matter',     10),
        -- 2nd house
        ( 2,'H02_WEALTH',            N'Wealth and accumulated assets',                       'Matter',      1),
        ( 2,'H02_FAMILY',            N'Family (kutumba)',                                     'Person',      2),
        ( 2,'H02_SPEECH',            N'Speech',                                              'Matter',      3),
        ( 2,'H02_EYES',              N'Eyes, especially the right eye',                      'BodyPart',    4),
        ( 2,'H02_MOUTH_FACE',        N'Mouth and face',                                     'BodyPart',    5),
        ( 2,'H02_VOICE',             N'Voice',                                              'Matter',      6),
        ( 2,'H02_FOOD',              N'Food and eating',                                     'Matter',      7),
        -- 3rd house
        ( 3,'H03_YOUNGER_COBORNS',   N'Younger siblings and co-borns',                       'Person',      1),
        ( 3,'H03_CONFIDANTS',        N'Confidants',                                          'Person',      2),
        ( 3,'H03_COURAGE',           N'Courage and valour',                                  'Matter',      3),
        ( 3,'H03_MENTAL_STRENGTH',   N'Mental strength and resolve',                         'Matter',      4),
        ( 3,'H03_COMMUNICATION',     N'Communication skills',                                'Matter',      5),
        ( 3,'H03_CREATIVITY',        N'Creativity and skill with the hands',                 'Matter',      6),
        ( 3,'H03_ARMS',              N'Arms and shoulders',                                  'BodyPart',    7),
        ( 3,'H03_THROAT_EARS',       N'Throat and ears',                                     'BodyPart',    8),
        ( 3,'H03_SHORT_TRAVELS',     N'Short journeys and travels',                          'Matter',      9),
        ( 3,'H03_FATHERS_DEATH',     N'Father''s death (7th from the 9th)',                  'DerivedHouse',10),
        ( 3,'H03_VEHICLE_HOUSE_EXPENSE', N'Expenditure on vehicles and house (12th from the 4th)', 'DerivedHouse', 11),
        -- 4th house
        ( 4,'H04_MOTHER',            N'Mother',                                              'Person',      1),
        ( 4,'H04_HAPPINESS',         N'Happiness, comforts and peace of mind',               'Matter',      2),
        ( 4,'H04_VEHICLES',          N'Vehicles',                                            'Matter',      3),
        ( 4,'H04_HOUSE_LANDS',       N'House, lands and immovable property',                 'Matter',      4),
        ( 4,'H04_MOTHERLAND',        N'Motherland',                                          'Matter',      5),
        ( 4,'H04_CHILDHOOD',         N'Childhood',                                           'Matter',      6),
        ( 4,'H04_EDUCATION',         N'Education',                                           'Matter',      7),
        ( 4,'H04_HEART',             N'Heart and chest',                                     'BodyPart',    8),
        ( 4,'H04_STATE_OF_MIND',     N'State of mind and emotional foundation',              'Matter',      9),
        ( 4,'H04_REALESTATE_WEALTH', N'Wealth from real estate',                             'Matter',     10),
        ( 4,'H04_RELATIVES',         N'Relatives',                                           'Person',     11),
        -- 5th house
        ( 5,'H05_CHILDREN',          N'Children',                                            'Person',      1),
        ( 5,'H05_POORVAPUNYA',       N'Merit of past deeds (poorvapunya)',                   'Matter',      2),
        ( 5,'H05_INTELLIGENCE',      N'Intelligence and discrimination',                     'Matter',      3),
        ( 5,'H05_KNOWLEDGE',         N'Knowledge and scholarship',                           'Matter',      4),
        ( 5,'H05_MANTRA',            N'Devotion, mantras and worship',                       'Matter',      5),
        ( 5,'H05_STOMACH',           N'Stomach and digestive system',                        'BodyPart',    6),
        ( 5,'H05_AUTHORITY',         N'Authority and power',                                 'Matter',      7),
        ( 5,'H05_FAME',              N'Fame',                                                'Matter',      8),
        ( 5,'H05_LOVE_EMOTIONS',     N'Love, affection and emotions',                        'Matter',      9),
        ( 5,'H05_JUDGMENT',          N'Judgment',                                            'Matter',     10),
        ( 5,'H05_SPECULATION',       N'Speculation',                                         'Matter',     11),
        -- 6th house
        ( 6,'H06_ENEMIES',           N'Enemies',                                             'Person',      1),
        ( 6,'H06_SERVICE',           N'Service and employment under others',                 'Matter',      2),
        ( 6,'H06_SERVANTS',          N'Servants and subordinates',                           'Person',      3),
        ( 6,'H06_RELATIVES',         N'Relatives',                                           'Person',      4),
        ( 6,'H06_MENTAL_TENSION',    N'Mental tension and affliction',                       'Matter',      5),
        ( 6,'H06_INJURIES',          N'Injuries and accidents',                              'Matter',      6),
        ( 6,'H06_DISEASE',           N'Diseases and health troubles',                        'Matter',      7),
        ( 6,'H06_AGRICULTURE',       N'Agriculture',                                         'Matter',      8),
        ( 6,'H06_MATERNAL_UNCLE',    N'Mother''s younger brother',                           'Person',      9),
        ( 6,'H06_HIPS',              N'Hips',                                                'BodyPart',   10),
        -- 7th house
        ( 7,'H07_MARRIAGE',          N'Marriage and marital life',                           'Matter',      1),
        ( 7,'H07_SPOUSE',            N'Life partner and spouse',                             'Person',      2),
        ( 7,'H07_PASSION',           N'Sex and passion',                                     'Matter',      3),
        ( 7,'H07_LONG_JOURNEYS',     N'Long journeys',                                       'Matter',      4),
        ( 7,'H07_PARTNERS',          N'Partners and partnerships',                           'Person',      5),
        ( 7,'H07_BUSINESS',          N'Business and trade',                                  'Matter',      6),
        ( 7,'H07_DEATH',             N'Death (a maraka house)',                              'Matter',      7),
        ( 7,'H07_BELOW_NAVEL',       N'Portion of the body below the navel',                 'BodyPart',    8),
        -- 8th house
        ( 8,'H08_LONGEVITY',         N'Longevity and lifespan',                              'Matter',      1),
        ( 8,'H08_DEBTS',             N'Debts',                                               'Matter',      2),
        ( 8,'H08_DISEASE',           N'Chronic and lingering disease',                       'Matter',      3),
        ( 8,'H08_ILL_FAME',          N'Ill fame and disgrace',                               'Matter',      4),
        ( 8,'H08_INHERITANCE',       N'Inheritance and legacies',                            'Matter',      5),
        ( 8,'H08_LOSS_OF_FRIENDS',   N'Loss of friends',                                     'Matter',      6),
        ( 8,'H08_OCCULT',            N'Occult studies',                                      'Matter',      7),
        ( 8,'H08_UNEARNED_WEALTH',   N'Unearned wealth, windfalls and gifts',                'Matter',      8),
        ( 8,'H08_SECRETS',           N'Secrets and hidden matters',                          'Matter',      9),
        ( 8,'H08_GENITALS',          N'Genitals',                                            'BodyPart',   10),
        -- 9th house
        ( 9,'H09_FATHER',            N'Father',                                              'Person',      1),
        ( 9,'H09_GURU',              N'Teacher and guru',                                    'Person',      2),
        ( 9,'H09_BOSS',              N'Boss and superior',                                   'Person',      3),
        ( 9,'H09_FORTUNE',           N'Fortune and luck',                                    'Matter',      4),
        ( 9,'H09_DHARMA',            N'Dharma, religiousness and principles',                'Matter',      5),
        ( 9,'H09_GOD',               N'God and grace',                                       'Matter',      6),
        ( 9,'H09_HIGHER_STUDIES',    N'Higher studies and high knowledge',                   'Matter',      7),
        ( 9,'H09_FOREIGN_FORTUNE',   N'Fortune and trips in a foreign land',                 'Matter',      8),
        ( 9,'H09_DIKSHA',            N'Diksha and spiritual initiation',                     'Matter',      9),
        ( 9,'H09_PAST_LIFE',         N'Past life and the cause of the present birth',        'Matter',     10),
        ( 9,'H09_GRANDCHILDREN',     N'Grandchildren',                                       'Person',     11),
        ( 9,'H09_INTUITION',         N'Intuition, compassion and sympathy',                  'Matter',     12),
        ( 9,'H09_CHARITY',           N'Charity and leadership',                              'Matter',     13),
        ( 9,'H09_THIGHS',            N'Thighs',                                              'BodyPart',   14),
        -- 10th house
        (10,'H10_CAREER',            N'Profession and career',                               'Matter',      1),
        (10,'H10_KARMA',             N'Action and karma',                                    'Matter',      2),
        (10,'H10_GROWTH',            N'Growth and rise in life',                             'Matter',      3),
        (10,'H10_CONDUCT',           N'Conduct in society',                                  'Matter',      4),
        (10,'H10_FAME_HONOURS',      N'Fame and honours',                                    'Matter',      5),
        (10,'H10_AWARDS',            N'Awards and recognition',                              'Matter',      6),
        (10,'H10_SELF_RESPECT',      N'Self-respect and dignity',                            'Matter',      7),
        (10,'H10_KNEES',             N'Knees',                                               'BodyPart',    8),
        -- 11th house
        (11,'H11_ELDER_COBORNS',     N'Elder siblings and co-borns',                         'Person',      1),
        (11,'H11_INCOME',            N'Income and gains',                                    'Matter',      2),
        (11,'H11_REALIZATION_OF_HOPES', N'Realization of hopes and desires',                 'Matter',      3),
        (11,'H11_FRIENDS',           N'Friends',                                             'Person',      4),
        (11,'H11_ANKLES',            N'Ankles',                                              'BodyPart',    5),
        -- 12th house
        (12,'H12_LOSSES',            N'Losses',                                              'Matter',      1),
        (12,'H12_EXPENDITURE',       N'Expenditure',                                         'Matter',      2),
        (12,'H12_PUNISHMENT',        N'Punishment and imprisonment',                         'Matter',      3),
        (12,'H12_HOSPITALIZATION',   N'Hospitalization',                                     'Matter',      4),
        (12,'H12_BED_PLEASURES',     N'Pleasures of the bed',                                'Matter',      5),
        (12,'H12_MISFORTUNE',        N'Misfortune',                                          'Matter',      6),
        (12,'H12_BAD_HABITS',        N'Bad habits',                                          'Matter',      7),
        (12,'H12_SLEEP',             N'Sleep',                                               'Matter',      8),
        (12,'H12_MEDITATION',        N'Meditation',                                          'Matter',      9),
        (12,'H12_DONATION',          N'Donation and giving',                                 'Matter',     10),
        (12,'H12_SECRET_ENEMIES',    N'Secret enemies',                                      'Person',     11),
        (12,'H12_HEAVEN',            N'Heaven',                                              'Matter',     12),
        (12,'H12_LEFT_EYE',          N'Left eye',                                            'BodyPart',   13),
        (12,'H12_FEET',              N'Feet',                                                'BodyPart',   14),
        (12,'H12_FOREIGN_RESIDENCE', N'Residence away from the birthplace',                  'Matter',     15),
        (12,'H12_MOKSHA',            N'Liberation (moksha)',                                 'Matter',     16)
        ) v (HouseNumber, SignificationCode, SignificationText, SignificationCategory, DisplayOrder)
    )
    INSERT dbo.tbl_Rule_HouseSignification
        (RuleSetId, HouseNumber, SignificationCode, SignificationText, SignificationCategory,
         DisplayOrder, MethodCode, SourceRefCode, IsActive)
    SELECT 1, s.HouseNumber, s.SignificationCode, s.SignificationText, s.SignificationCategory,
           s.DisplayOrder, 'MAP_LOOKUP', 'SRC_PVR_INTEGRATED', 1
    FROM s;
END
GO

-- --- Batch 4: tbl_Dim_HouseAttribute (attribute catalogue) ---
IF OBJECT_ID('dbo.tbl_Dim_HouseAttribute', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_HouseAttribute (
        AttributeCode VARCHAR(30)   NOT NULL CONSTRAINT PK_Dim_HouseAttribute PRIMARY KEY,
        DisplayName   NVARCHAR(80)  NOT NULL,
        ValueKind     VARCHAR(10)   NOT NULL CONSTRAINT CK_Dim_HouseAttribute_Kind CHECK (ValueKind IN ('CODE','TEXT','NUMBER')),
        SortOrder     TINYINT       NOT NULL,
        Notes         NVARCHAR(400) NULL
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_HouseAttribute)
    INSERT dbo.tbl_Dim_HouseAttribute (AttributeCode, DisplayName, ValueKind, SortOrder, Notes)
    VALUES
    ('GENERAL_CHARACTER',    N'General character of the bhava', 'TEXT', 1, N'Free-text disposition / summary of the house.'),
    ('CATEGORY_EFFECT',      N'Special-category reading',       'TEXT', 2, N'PVR sec 7.4.6 quick-summary reading implied by the house''s dominant special category.'),
    ('NATURAL_SIGNIFICATOR', N'Naisargika bhava karaka',       'CODE', 3, N'Planet(s) that are the natural significators of the house; multi-valued via Priority. Seeded from PVR Table 12 in migration 32.');
GO

-- --- Batch 5: tbl_Rule_HouseAttribute (EAV, mirror of tbl_Rule_GrahaAttribute) ---
IF OBJECT_ID('dbo.tbl_Rule_HouseAttribute', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_HouseAttribute (
        Id            INT IDENTITY(1,1) NOT NULL
                          CONSTRAINT PK_Rule_HouseAttribute PRIMARY KEY,
        RuleSetId     TINYINT      NOT NULL
                          CONSTRAINT FK_Rule_HouseAttribute_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        HouseNumber   TINYINT      NOT NULL
                          CONSTRAINT FK_Rule_HouseAttribute_House FOREIGN KEY REFERENCES dbo.tbl_Dim_House (HouseNumber),
        AttributeCode VARCHAR(30)  NOT NULL
                          CONSTRAINT FK_Rule_HouseAttribute_Attr FOREIGN KEY REFERENCES dbo.tbl_Dim_HouseAttribute (AttributeCode),
        ValueCode     VARCHAR(40)   NULL,       -- canonical token; NULL for TEXT attributes
        ValueText     NVARCHAR(400) NOT NULL,   -- readable value (always present)
        Priority      TINYINT      NOT NULL CONSTRAINT DF_Rule_HouseAttribute_Priority DEFAULT 1,
        SourceRefCode VARCHAR(40)   NULL,
        IsActive      BIT          NOT NULL CONSTRAINT DF_Rule_HouseAttribute_IsActive DEFAULT 1,
        Notes         NVARCHAR(400) NULL,
        CONSTRAINT CK_RuleHouseAttr_Src CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_HouseAttribute UNIQUE (RuleSetId, HouseNumber, AttributeCode, Priority)
    );
    CREATE NONCLUSTERED INDEX IX_Rule_HouseAttribute_Attr
        ON dbo.tbl_Rule_HouseAttribute (AttributeCode, HouseNumber);
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_HouseAttribute)
    INSERT dbo.tbl_Rule_HouseAttribute
        (RuleSetId, HouseNumber, AttributeCode, ValueCode, ValueText, Priority, SourceRefCode, IsActive)
    SELECT 1, v.HouseNumber, v.AttributeCode, NULL, v.ValueText, 1, 'SRC_PVR_INTEGRATED', 1
    FROM (VALUES
    ( 1,'GENERAL_CHARACTER', N'The body and the self; the lens through which the whole chart is read.'),
    ( 2,'GENERAL_CHARACTER', N'Resources drawn to the self - wealth, family, nourishment and speech.'),
    ( 3,'GENERAL_CHARACTER', N'Self-driven effort - courage, skill, siblings and the will to act.'),
    ( 4,'GENERAL_CHARACTER', N'The inner base - mother, home, land and emotional security.'),
    ( 5,'GENERAL_CHARACTER', N'Creative merit - children, intelligence, devotion and past-life credit.'),
    ( 6,'GENERAL_CHARACTER', N'Friction and service - enemies, debt, disease and daily work.'),
    ( 7,'GENERAL_CHARACTER', N'The other - spouse, partners, desire and dealings with the world.'),
    ( 8,'GENERAL_CHARACTER', N'Rupture and depth - longevity, the occult, inheritance and hidden things.'),
    ( 9,'GENERAL_CHARACTER', N'Grace and guidance - father, guru, fortune and higher dharma.'),
    (10,'GENERAL_CHARACTER', N'Action in the world - career, karma, conduct and public standing.'),
    (11,'GENERAL_CHARACTER', N'Fulfilment - income, gains, friends and elder siblings.'),
    (12,'GENERAL_CHARACTER', N'Dissolution - loss, expense, seclusion, foreign lands and moksha.'),
    ( 1,'CATEGORY_EFFECT',   N'Trine and quadrant: prosperity, dharma and vital activity all rest on the 1st.'),
    ( 2,'CATEGORY_EFFECT',   N'Succedent and a maraka: holds accumulated wealth and family; a death-inflicting house.'),
    ( 3,'CATEGORY_EFFECT',   N'Upachaya: courage, effort and skills grow with time.'),
    ( 4,'CATEGORY_EFFECT',   N'Quadrant: sustenance, home and peace of mind; a pillar of the chart.'),
    ( 5,'CATEGORY_EFFECT',   N'Trine: merit, intelligence and progeny; an abode of Lakshmi.'),
    ( 6,'CATEGORY_EFFECT',   N'Upachaya dusthana: enemies and disease, but competitive strength grows over time.'),
    ( 7,'CATEGORY_EFFECT',   N'Quadrant and a maraka: partnership and desire; a death-inflicting house.'),
    ( 8,'CATEGORY_EFFECT',   N'Dusthana: upheaval, the hidden and longevity; setbacks and transformation.'),
    ( 9,'CATEGORY_EFFECT',   N'Trine: fortune, dharma and the guru; the strongest trine after the 1st.'),
    (10,'CATEGORY_EFFECT',   N'Quadrant and upachaya: action and status that build through sustained effort.'),
    (11,'CATEGORY_EFFECT',   N'Upachaya: gains and fulfilled desires that accumulate over time.'),
    (12,'CATEGORY_EFFECT',   N'Dusthana: loss and expense, turned toward liberation and the beyond.')
    ) v (HouseNumber, AttributeCode, ValueText);
GO

-- --- Batch 6: tbl_Rule_Catalog (new + reserved-row refresh) ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_HouseAttribute')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_HouseAttribute', 'HOUSE', 'ATTR_LOOKUP',
            'Per-house descriptive attributes (PVR ch. 7): general character, sec 7.4.6 special-category effect, and naisargika bhava karaka. One row per (rule-set, house, attribute, priority); mirror of tbl_Rule_GrahaAttribute.',
            '31_add_house_model.sql');
GO
UPDATE dbo.tbl_Rule_Catalog
   SET MethodCodes  = 'MAP_LOOKUP',
       Purpose      = 'Bhava karatvas: the significations attached to each house (PVR sec 7.2). One row per matter, categorised body-part / person / matter / derived-house, RuleSetId 1.',
       IntroducedIn = '31_add_house_model.sql'
 WHERE RuleTableName = 'tbl_Rule_HouseSignification';
GO

-- --- Batch 7: taxonomy (addendum - not yet emitted by TerminologySeed.cs) ---
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Astro_Terminology_Category')
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE name = 'CK_Astro_Terminology_Category' AND definition LIKE '%HouseCategory%')
BEGIN
    ALTER TABLE dbo.tbl_Astro_Terminology DROP CONSTRAINT CK_Astro_Terminology_Category;
    ALTER TABLE dbo.tbl_Astro_Terminology ADD CONSTRAINT CK_Astro_Terminology_Category CHECK (Category IN (
        'Planet','Sign','House','Nakshatra','NakshatraPada','DivisionalChart','Karaka',
        'SpecialPoint','AvasthaState','DignityState','Relationship','StrengthComponent',
        'Dasha','Yoga','Ayanamsa','Concept','LifeArea','HouseCategory'));
END
GO
MERGE dbo.tbl_Astro_Terminology AS tgt
USING (
    SELECT 'HouseCategory' AS Category, 'HCAT_' + CategoryCode AS Code,
           CONVERT(VARCHAR(40), NULL) AS ParentCode, 'HOUSE' AS EngineCode,
           CONVERT(INT, NULL) AS NumericKey, 969 + SortOrder AS DisplayOrder
    FROM dbo.tbl_Dim_HouseCategory
    UNION ALL
    SELECT * FROM (VALUES
        ('Concept','PURUSHARTHA_DHARMA', CONVERT(VARCHAR(40),NULL),'HOUSE',CONVERT(INT,NULL),980),
        ('Concept','PURUSHARTHA_ARTHA',  NULL,'HOUSE',NULL,981),
        ('Concept','PURUSHARTHA_KAMA',   NULL,'HOUSE',NULL,982),
        ('Concept','PURUSHARTHA_MOKSHA', NULL,'HOUSE',NULL,983),
        ('Concept','ZHALF_VISIBLE',      NULL,'HOUSE',NULL,985),
        ('Concept','ZHALF_INVISIBLE',    NULL,'HOUSE',NULL,986)
    ) p (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder)
) AS src (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder)
ON tgt.Code = src.Code
WHEN MATCHED THEN UPDATE SET Category = src.Category, ParentCode = src.ParentCode,
    EngineCode = src.EngineCode, NumericKey = src.NumericKey, DisplayOrder = src.DisplayOrder, IsActive = 1
WHEN NOT MATCHED THEN INSERT (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder, IsActive)
    VALUES (src.Category, src.Code, src.ParentCode, src.EngineCode, src.NumericKey, src.DisplayOrder, 1);
GO
MERGE dbo.tbl_Astro_TerminologyText AS tgt
USING (
  SELECT t.TerminologyId, v.LanguageCode, v.Script, v.Name, v.TraditionalName, v.ShortDescription
  FROM (VALUES
   ('HCAT_KENDRA','sa','Latn',N'Kendra',N'Kendra',NULL),
   ('HCAT_KENDRA','en','Latn',N'Quadrant houses',NULL,N'Houses 1, 4, 7, 10 - the angular houses; sustenance and vital activity, the pillars of the chart (PVR sec 7.4.2).'),
   ('HCAT_TRIKONA','sa','Latn',N'Trikona',N'Trikona',NULL),
   ('HCAT_TRIKONA','en','Latn',N'Trine houses',NULL,N'Houses 1, 5, 9 - the trines; prosperity, dharma and grace, an abode of Lakshmi (PVR sec 7.4.1).'),
   ('HCAT_PANAPHARA','sa','Latn',N'Panaphara',N'Panaphara',NULL),
   ('HCAT_PANAPHARA','en','Latn',N'Succedent houses',NULL,N'Houses 2, 5, 8, 11 - the quadrants counted from the 2nd; accumulation and holding (PVR sec 7.4.3).'),
   ('HCAT_APOKLIMA','sa','Latn',N'Apoklima',N'Apoklima',NULL),
   ('HCAT_APOKLIMA','en','Latn',N'Cadent houses',NULL,N'Houses 3, 6, 9, 12 - the quadrants counted from the 3rd; seeking, effort and dissipation (PVR sec 7.4.4).'),
   ('HCAT_UPACHAYA','sa','Latn',N'Upachaya',N'Upachaya',NULL),
   ('HCAT_UPACHAYA','en','Latn',N'Houses of growth',NULL,N'Houses 3, 6, 10, 11 - matters that improve with time and sustained effort (PVR sec 7.4).'),
   ('HCAT_DUSTHANA','sa','Latn',N'Dusthana',N'Trik Sthana',NULL),
   ('HCAT_DUSTHANA','en','Latn',N'Trik (evil) houses',NULL,N'Houses 6, 8, 12 - setbacks, suffering, loss and dissolution (PVR sec 7.4).'),
   ('HCAT_CHATURASRA','sa','Latn',N'Chaturasra',N'Chaturasra',NULL),
   ('HCAT_CHATURASRA','en','Latn',N'Quadrangle houses',NULL,N'Houses 4 and 8 together; latent strain around home and longevity (PVR sec 7.4).'),
   ('HCAT_MARAKA','sa','Latn',N'Maraka',N'Maraka',NULL),
   ('HCAT_MARAKA','en','Latn',N'Maraka (killer) houses',NULL,N'Houses 2 and 7 - the death-inflicting houses; from the longevity chapter, not PVR sec 7.4.'),
   ('PURUSHARTHA_DHARMA','sa','Latn',N'Dharma',N'Dharma Trikona',NULL),
   ('PURUSHARTHA_DHARMA','en','Latn',N'Dharma trikona',NULL,N'Houses 1, 5, 9 counted from the 1st - purpose, righteousness and meaning (PVR sec 7.4.1).'),
   ('PURUSHARTHA_ARTHA','sa','Latn',N'Artha',N'Artha Trikona',NULL),
   ('PURUSHARTHA_ARTHA','en','Latn',N'Artha trikona',NULL,N'Houses 2, 6, 10 counted from the 2nd - wealth, work and material security (PVR sec 7.4.1).'),
   ('PURUSHARTHA_KAMA','sa','Latn',N'Kama',N'Kama Trikona',NULL),
   ('PURUSHARTHA_KAMA','en','Latn',N'Kama trikona',NULL,N'Houses 3, 7, 11 counted from the 3rd - desire, relationship and fulfilment (PVR sec 7.4.1).'),
   ('PURUSHARTHA_MOKSHA','sa','Latn',N'Moksha',N'Moksha Trikona',NULL),
   ('PURUSHARTHA_MOKSHA','en','Latn',N'Moksha trikona',NULL,N'Houses 4, 8, 12 counted from the 4th - release, dissolution and liberation (PVR sec 7.4.1).'),
   ('ZHALF_VISIBLE','sa','Latn',N'Drishya Bhaga',N'Drishya Bhaga',NULL),
   ('ZHALF_VISIBLE','en','Latn',N'Visible half',NULL,N'Houses 7 to 12 - the half of the zodiac above the horizon at the moment of birth (PVR sec 7.4.5).'),
   ('ZHALF_INVISIBLE','sa','Latn',N'Adrishya Bhaga',N'Adrishya Bhaga',NULL),
   ('ZHALF_INVISIBLE','en','Latn',N'Invisible half',NULL,N'Houses 1 to 6 - the half of the zodiac below the horizon at the moment of birth (PVR sec 7.4.5).')
  ) AS v (Code, LanguageCode, Script, Name, TraditionalName, ShortDescription)
  JOIN dbo.tbl_Astro_Terminology t ON t.Code = v.Code
) AS src
ON tgt.TerminologyId = src.TerminologyId AND tgt.LanguageCode = src.LanguageCode AND tgt.Script = src.Script
WHEN MATCHED THEN UPDATE SET Name = src.Name, TraditionalName = src.TraditionalName, ShortDescription = src.ShortDescription
WHEN NOT MATCHED THEN INSERT (TerminologyId, LanguageCode, Script, Name, TraditionalName, ShortDescription)
    VALUES (src.TerminologyId, src.LanguageCode, src.Script, src.Name, src.TraditionalName, src.ShortDescription);
GO

-- --- Batch 8: ledger + summary ---
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '31_add_house_model.sql',
       'tbl_Dim_House(12)+tbl_Dim_HouseCategory(8); tbl_Rule_HouseSignification populated (121, sec 7.2); tbl_Dim_HouseAttribute(3)/tbl_Rule_HouseAttribute(24); taxonomy +14 concepts/28 text.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '31_add_house_model.sql');
GO

DECLARE @houses  INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_House);
DECLARE @cats    INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_HouseCategory);
DECLARE @sig     INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_HouseSignification);
DECLARE @sighouses INT = (SELECT COUNT(DISTINCT HouseNumber) FROM dbo.tbl_Rule_HouseSignification);
DECLARE @attrdim INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_HouseAttribute);
DECLARE @attr    INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_HouseAttribute);
DECLARE @orphan  INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_HouseAttribute r
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_HouseAttribute d WHERE d.AttributeCode = r.AttributeCode));
DECLARE @cat     INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_Catalog WHERE RuleTableName IN ('tbl_Rule_HouseSignification','tbl_Rule_HouseAttribute'));
DECLARE @concepts INT = (SELECT COUNT(*) FROM dbo.tbl_Astro_Terminology
    WHERE Category = 'HouseCategory' OR Code LIKE 'PURUSHARTHA[_]%' OR Code LIKE 'ZHALF[_]%');
DECLARE @notext  INT = (SELECT COUNT(*) FROM dbo.tbl_Astro_Terminology t
    WHERE (t.Category = 'HouseCategory' OR t.Code LIKE 'PURUSHARTHA[_]%' OR t.Code LIKE 'ZHALF[_]%')
      AND (NOT EXISTS (SELECT 1 FROM dbo.tbl_Astro_TerminologyText x WHERE x.TerminologyId = t.TerminologyId AND x.LanguageCode = 'sa')
        OR NOT EXISTS (SELECT 1 FROM dbo.tbl_Astro_TerminologyText x WHERE x.TerminologyId = t.TerminologyId AND x.LanguageCode = 'en')));
PRINT '31 applied: ' + CAST(@houses AS VARCHAR(10)) + ' houses (expect 12), '
    + CAST(@cats AS VARCHAR(10)) + ' categories (expect 8), '
    + CAST(@sig AS VARCHAR(10)) + ' significations across ' + CAST(@sighouses AS VARCHAR(10)) + ' houses (expect 12), '
    + CAST(@attrdim AS VARCHAR(10)) + ' attr catalog (expect 3), '
    + CAST(@attr AS VARCHAR(10)) + ' attr rows (expect 24), '
    + CAST(@orphan AS VARCHAR(10)) + ' orphan attr codes (expect 0), '
    + CAST(@cat AS VARCHAR(10)) + ' rule-catalog rows (expect 2), '
    + CAST(@concepts AS VARCHAR(10)) + ' taxonomy concepts (expect 14), '
    + CAST(@notext AS VARCHAR(10)) + ' concepts missing sa or en text (expect 0).';
GO
