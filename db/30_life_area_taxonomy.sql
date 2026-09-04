-- =====================================================================
-- 30 - Life-area taxonomy: the spheres of life a chart is read for.
--
-- The Web workspace was first planned around four coarse buckets
-- (PersonalityHealth / Relationships / Career / Money - src/Ikiastrro.Core/
-- LifeAreas/LifeAreaMap.cs). Introducing the divisional-chart correlation
-- (migration 29) needs a finer set: PVR "Integrated Approach" Table 11
-- (sec 6.3) gives one primary area of life per divisional chart. This
-- migration turns that into a first-class dimension and wires the two
-- related tables + the taxonomy to it.
--
--   tbl_Dim_LifeArea         - 20 rows, one per PVR Table 11 sphere.
--                              PlaneOfExistence is PVR sec 6.4 (physical /
--                              mental / sub-conscious / karmic).
--                              WorkspaceGroupCode is the PROJECT Web-tab
--                              grouping (LifeAreaMap.cs) rolled onto the
--                              fine areas - not from the book.
--   tbl_Dim_ChartType        += PrimaryLifeAreaId (FK) - every one of the
--                              21 position charts mapped; D2-US shares
--                              D2's area (Wealth).
--   tbl_Dim_SpecialLagnas    LifeAreaFocus (free text, migration 29) is
--                              replaced by LifeAreaId (FK). Hora Lagna ->
--                              Wealth, Ghati Lagna -> Fame/authority/power
--                              (Table 11 D-5), Sree Lagna -> Wealth
--                              (prosperity). Ghati Lagna's RelatedVargaChartId
--                              is corrected D-10 -> D-5 to match its own
--                              signification (D-10 stays a secondary read,
--                              noted in UsageContext).
--   tbl_Astro_Terminology    += Category 'LifeArea'; 20 LifeArea concepts
--                              + 4 plane-of-existence Concepts, each with
--                              sa + en text (addendum, as migration 29 -
--                              TerminologySeed.cs does not emit these yet).
--
-- RuleSetId n/a (reference dimension). SourceRefCode SRC_PVR_INTEGRATED on
-- the tbl_Dim_LifeArea rows. Idempotent throughout.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/30_life_area_taxonomy.sql
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- --- Batch 1: tbl_Dim_LifeArea (master, PVR Table 11) ---
IF OBJECT_ID('dbo.tbl_Dim_LifeArea', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_LifeArea (
        Id                 TINYINT       NOT NULL
                               CONSTRAINT PK_Dim_LifeArea PRIMARY KEY,        -- fixed 20-row domain
        AreaCode           VARCHAR(24)   NOT NULL
                               CONSTRAINT UQ_Dim_LifeArea_Code UNIQUE,        -- WEALTH, SIBLINGS, ...
        AreaName           VARCHAR(40)   NOT NULL
                               CONSTRAINT UQ_Dim_LifeArea_Name UNIQUE,
        Description        NVARCHAR(200)  NOT NULL,                            -- the PVR Table 11 phrasing
        PlaneOfExistence   VARCHAR(12)    NOT NULL,                            -- PVR sec 6.4
        WorkspaceGroupCode VARCHAR(20)    NOT NULL,                            -- project Web-tab grouping (LifeAreaMap.cs), NOT from PVR
        SortOrder          TINYINT        NOT NULL,
        IsActive           BIT            NOT NULL CONSTRAINT DF_Dim_LifeArea_IsActive DEFAULT 1,
        SourceRefCode      VARCHAR(40)    NULL,
        CONSTRAINT CK_Dim_LifeArea_Plane CHECK (PlaneOfExistence IN ('Physical','Mental','SubConscious','Karmic')),
        CONSTRAINT CK_Dim_LifeArea_Group CHECK (WorkspaceGroupCode IN ('PersonalityHealth','Relationships','Career','Money','Other')),
        CONSTRAINT CK_Dim_LifeArea_Src   CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_LifeArea)
    INSERT dbo.tbl_Dim_LifeArea
        (Id, AreaCode, AreaName, Description, PlaneOfExistence, WorkspaceGroupCode, SortOrder, SourceRefCode)
    VALUES
        ( 1,'PHYSICAL_EXISTENCE',   'Physical existence',        N'Existence at the physical level; general matters (PVR Table 11, D-1).',                       'Physical',    'PersonalityHealth',  1,'SRC_PVR_INTEGRATED'),
        ( 2,'WEALTH',               'Wealth and money',          N'Wealth and money (PVR Table 11, D-2).',                                                       'Physical',    'Money',              2,'SRC_PVR_INTEGRATED'),
        ( 3,'SIBLINGS',             'Siblings',                  N'Everything related to brothers and sisters (PVR Table 11, D-3).',                             'Physical',    'Relationships',      3,'SRC_PVR_INTEGRATED'),
        ( 4,'PROPERTY_FORTUNE',     'Property and fortune',      N'Residence, houses owned, properties and fortune (PVR Table 11, D-4).',                        'Physical',    'Money',              4,'SRC_PVR_INTEGRATED'),
        ( 5,'FAME_POWER',           'Fame, authority and power', N'Fame, authority and power (PVR Table 11, D-5).',                                              'Physical',    'Career',             5,'SRC_PVR_INTEGRATED'),
        ( 6,'HEALTH_TROUBLES',      'Health troubles',           N'Health troubles (PVR Table 11, D-6).',                                                        'Physical',    'PersonalityHealth',  6,'SRC_PVR_INTEGRATED'),
        ( 7,'CHILDREN',             'Children',                  N'Everything related to children and grand-children (PVR Table 11, D-7).',                      'Physical',    'Relationships',      7,'SRC_PVR_INTEGRATED'),
        ( 8,'SUDDEN_TROUBLES',      'Sudden troubles',           N'Sudden and unexpected troubles, litigation etc (PVR Table 11, D-8).',                         'Physical',    'PersonalityHealth',  8,'SRC_PVR_INTEGRATED'),
        ( 9,'MARRIAGE_SPOUSE',      'Marriage and spouse',       N'Marriage and everything related to spouse(s); dharma, interaction with others, basic skills, inner self (PVR Table 11, D-9).', 'Physical','Relationships', 9,'SRC_PVR_INTEGRATED'),
        (10,'CAREER',               'Career and achievements',   N'Career, activities and achievements in society (PVR Table 11, D-10).',                        'Physical',    'Career',            10,'SRC_PVR_INTEGRATED'),
        (11,'DEATH_DESTRUCTION',    'Death and destruction',     N'Death and destruction (PVR Table 11, D-11).',                                                 'Physical',    'PersonalityHealth', 11,'SRC_PVR_INTEGRATED'),
        (12,'PARENTS',              'Parents',                   N'Everything related to parents, and to uncles, aunts and grand-parents - the blood-relatives of parents (PVR Table 11, D-12).', 'Physical','Relationships', 12,'SRC_PVR_INTEGRATED'),
        (13,'VEHICLES_COMFORTS',    'Vehicles and comforts',     N'Vehicles, pleasures, comforts and discomforts (PVR Table 11, D-16).',                         'Mental',      'Other',             13,'SRC_PVR_INTEGRATED'),
        (14,'RELIGION_SPIRITUALITY','Religion and spirituality', N'Religious activities and spiritual matters (PVR Table 11, D-20).',                            'Mental',      'Other',             14,'SRC_PVR_INTEGRATED'),
        (15,'EDUCATION',            'Learning and education',     N'Learning, knowledge and education (PVR Table 11, D-24).',                                     'Mental',      'Career',            15,'SRC_PVR_INTEGRATED'),
        (16,'INNATE_NATURE',        'Strengths and inherent nature', N'Strengths and weaknesses, inherent nature (PVR Table 11, D-27).',                         'SubConscious','PersonalityHealth', 16,'SRC_PVR_INTEGRATED'),
        (17,'EVILS_PUNISHMENT',     'Evils and punishment',      N'Evils and punishment, the sub-conscious self, some diseases (PVR Table 11, D-30).',           'SubConscious','PersonalityHealth', 17,'SRC_PVR_INTEGRATED'),
        (18,'AUSPICIOUS_EVENTS',    'Auspicious and inauspicious events', N'Auspicious and inauspicious events (PVR Table 11, D-40).',                            'Karmic',      'Other',             18,'SRC_PVR_INTEGRATED'),
        (19,'ALL_MATTERS',          'All matters',               N'All matters (PVR Table 11, D-45).',                                                           'Karmic',      'Other',             19,'SRC_PVR_INTEGRATED'),
        (20,'PAST_LIFE_KARMA',      'Past-life karma',           N'Karma of past life; all matters (PVR Table 11, D-60).',                                       'Karmic',      'Other',             20,'SRC_PVR_INTEGRATED');
GO

-- --- Batch 2: tbl_Dim_ChartType.PrimaryLifeAreaId ---
IF COL_LENGTH('dbo.tbl_Dim_ChartType', 'PrimaryLifeAreaId') IS NULL
    ALTER TABLE dbo.tbl_Dim_ChartType ADD PrimaryLifeAreaId TINYINT NULL
        CONSTRAINT FK_Dim_ChartType_PrimaryLifeArea FOREIGN KEY REFERENCES dbo.tbl_Dim_LifeArea (Id);
GO
UPDATE ct
   SET PrimaryLifeAreaId = la.Id
  FROM dbo.tbl_Dim_ChartType ct
  JOIN (VALUES
        ('D1','PHYSICAL_EXISTENCE'),   ('D2','WEALTH'),        ('D2-US','WEALTH'),
        ('D3','SIBLINGS'),             ('D4','PROPERTY_FORTUNE'), ('D5','FAME_POWER'),
        ('D6','HEALTH_TROUBLES'),      ('D7','CHILDREN'),      ('D8','SUDDEN_TROUBLES'),
        ('D9','MARRIAGE_SPOUSE'),      ('D10','CAREER'),       ('D11','DEATH_DESTRUCTION'),
        ('D12','PARENTS'),             ('D16','VEHICLES_COMFORTS'), ('D20','RELIGION_SPIRITUALITY'),
        ('D24','EDUCATION'),           ('D27','INNATE_NATURE'), ('D30','EVILS_PUNISHMENT'),
        ('D40','AUSPICIOUS_EVENTS'),   ('D45','ALL_MATTERS'),  ('D60','PAST_LIFE_KARMA')
       ) m (ChartCode, AreaCode) ON m.ChartCode = ct.Code
  JOIN dbo.tbl_Dim_LifeArea la ON la.AreaCode = m.AreaCode;
GO

-- --- Batch 3: tbl_Dim_SpecialLagnas - LifeAreaFocus (text) -> LifeAreaId (FK) ---
IF COL_LENGTH('dbo.tbl_Dim_SpecialLagnas', 'LifeAreaId') IS NULL
    ALTER TABLE dbo.tbl_Dim_SpecialLagnas ADD LifeAreaId TINYINT NULL
        CONSTRAINT FK_Dim_SpecialLagnas_LifeArea FOREIGN KEY REFERENCES dbo.tbl_Dim_LifeArea (Id);
GO
UPDATE l
   SET LifeAreaId = la.Id
  FROM dbo.tbl_Dim_SpecialLagnas l
  JOIN (VALUES ('HORA_LAGNA','WEALTH'), ('GHATI_LAGNA','FAME_POWER'), ('SREE_LAGNA','WEALTH')) m (LagnaCode, AreaCode)
       ON m.LagnaCode = l.LagnaCode
  JOIN dbo.tbl_Dim_LifeArea la ON la.AreaCode = m.AreaCode;
GO
-- Ghati Lagna's own signification is Table 11 D-5 (fame/authority/power);
-- correct the migration-29 pairing D-10 -> D-5. D-10 stays a secondary read.
UPDATE dbo.tbl_Dim_SpecialLagnas
   SET RelatedVargaChartId = (SELECT Id FROM dbo.tbl_Dim_ChartType WHERE Code = 'D5'),
       UsageContext = N'Bring in when timing fame / power / authority periods - PVR sec 5.6 weighs it heavily for someone in politics or public office. Its own sphere is D-5 (Panchamsa); read the D-10 (Dasamsa) career chart alongside it (PVR sec 6.5 promotion example). sec 5.5: GL shifts 1 deg 15 min per birthtime minute, so correct the birthtime before trusting it in vargas.'
 WHERE LagnaCode = 'GHATI_LAGNA';
GO
IF COL_LENGTH('dbo.tbl_Dim_SpecialLagnas', 'LifeAreaFocus') IS NOT NULL
    ALTER TABLE dbo.tbl_Dim_SpecialLagnas DROP COLUMN LifeAreaFocus;
GO

-- --- Batch 4: taxonomy - Category 'LifeArea' + concepts (addendum) ---
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Astro_Terminology_Category')
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE name = 'CK_Astro_Terminology_Category' AND definition LIKE '%LifeArea%')
BEGIN
    ALTER TABLE dbo.tbl_Astro_Terminology DROP CONSTRAINT CK_Astro_Terminology_Category;
    ALTER TABLE dbo.tbl_Astro_Terminology ADD CONSTRAINT CK_Astro_Terminology_Category CHECK (Category IN (
        'Planet','Sign','House','Nakshatra','NakshatraPada','DivisionalChart','Karaka',
        'SpecialPoint','AvasthaState','DignityState','Relationship','StrengthComponent',
        'Dasha','Yoga','Ayanamsa','Concept','LifeArea'));
END
GO
MERGE dbo.tbl_Astro_Terminology AS tgt
USING (
    SELECT 'LifeArea' AS Category, 'LIFEAREA_' + AreaCode AS Code,
           CONVERT(VARCHAR(40), NULL) AS ParentCode, 'LIFEAREA' AS EngineCode,
           CONVERT(INT, NULL) AS NumericKey, 920 + Id AS DisplayOrder
    FROM dbo.tbl_Dim_LifeArea
    UNION ALL
    SELECT * FROM (VALUES
        ('Concept','PLANE_PHYSICAL',     CONVERT(VARCHAR(40),NULL),'LIFEAREA',CONVERT(INT,NULL),961),
        ('Concept','PLANE_MENTAL',       NULL,'LIFEAREA',NULL,962),
        ('Concept','PLANE_SUBCONSCIOUS', NULL,'LIFEAREA',NULL,963),
        ('Concept','PLANE_KARMIC',       NULL,'LIFEAREA',NULL,964)
    ) p (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder)
) AS src (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder)
ON tgt.Code = src.Code
WHEN MATCHED THEN UPDATE SET Category = src.Category, ParentCode = src.ParentCode,
    EngineCode = src.EngineCode, NumericKey = src.NumericKey, DisplayOrder = src.DisplayOrder, IsActive = 1
WHEN NOT MATCHED THEN INSERT (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder, IsActive)
    VALUES (src.Category, src.Code, src.ParentCode, src.EngineCode, src.NumericKey, src.DisplayOrder, 1);
GO

-- --- Batch 5: taxonomy - sa + en text ---
-- en Name / ShortDescription from tbl_Dim_LifeArea; sa is a best-effort
-- Jyotish term as the reference for later ta / Deva translation.
MERGE dbo.tbl_Astro_TerminologyText AS tgt
USING (
  SELECT t.TerminologyId, v.LanguageCode, v.Script, v.Name, v.TraditionalName, v.ShortDescription
  FROM (VALUES
   ('LIFEAREA_PHYSICAL_EXISTENCE','sa','Latn',N'Sthula Deha',N'Sthula Deha',NULL),
   ('LIFEAREA_PHYSICAL_EXISTENCE','en','Latn',N'Physical existence',NULL,N'Existence at the physical level; general matters (PVR Table 11, D-1).'),
   ('LIFEAREA_WEALTH','sa','Latn',N'Dhana',N'Dhana',NULL),
   ('LIFEAREA_WEALTH','en','Latn',N'Wealth and money',NULL,N'Wealth and money (PVR Table 11, D-2).'),
   ('LIFEAREA_SIBLINGS','sa','Latn',N'Sahaja',N'Sahaja',NULL),
   ('LIFEAREA_SIBLINGS','en','Latn',N'Siblings',NULL,N'Everything related to brothers and sisters (PVR Table 11, D-3).'),
   ('LIFEAREA_PROPERTY_FORTUNE','sa','Latn',N'Griha Bhagya',N'Griha Bhagya',NULL),
   ('LIFEAREA_PROPERTY_FORTUNE','en','Latn',N'Property and fortune',NULL,N'Residence, houses owned, properties and fortune (PVR Table 11, D-4).'),
   ('LIFEAREA_FAME_POWER','sa','Latn',N'Yasha Adhikara',N'Yasha Adhikara',NULL),
   ('LIFEAREA_FAME_POWER','en','Latn',N'Fame, authority and power',NULL,N'Fame, authority and power (PVR Table 11, D-5).'),
   ('LIFEAREA_HEALTH_TROUBLES','sa','Latn',N'Roga',N'Roga',NULL),
   ('LIFEAREA_HEALTH_TROUBLES','en','Latn',N'Health troubles',NULL,N'Health troubles (PVR Table 11, D-6).'),
   ('LIFEAREA_CHILDREN','sa','Latn',N'Santana',N'Santana',NULL),
   ('LIFEAREA_CHILDREN','en','Latn',N'Children',NULL,N'Everything related to children and grand-children (PVR Table 11, D-7).'),
   ('LIFEAREA_SUDDEN_TROUBLES','sa','Latn',N'Akasmika Vighna',N'Akasmika Vighna',NULL),
   ('LIFEAREA_SUDDEN_TROUBLES','en','Latn',N'Sudden troubles',NULL,N'Sudden and unexpected troubles, litigation etc (PVR Table 11, D-8).'),
   ('LIFEAREA_MARRIAGE_SPOUSE','sa','Latn',N'Kalatra',N'Kalatra',NULL),
   ('LIFEAREA_MARRIAGE_SPOUSE','en','Latn',N'Marriage and spouse',NULL,N'Marriage and everything related to spouse(s); dharma, interaction with others, basic skills, inner self (PVR Table 11, D-9).'),
   ('LIFEAREA_CAREER','sa','Latn',N'Karma Ajiva',N'Karma Ajiva',NULL),
   ('LIFEAREA_CAREER','en','Latn',N'Career and achievements',NULL,N'Career, activities and achievements in society (PVR Table 11, D-10).'),
   ('LIFEAREA_DEATH_DESTRUCTION','sa','Latn',N'Mrityu Nasha',N'Mrityu Nasha',NULL),
   ('LIFEAREA_DEATH_DESTRUCTION','en','Latn',N'Death and destruction',NULL,N'Death and destruction (PVR Table 11, D-11).'),
   ('LIFEAREA_PARENTS','sa','Latn',N'Matri Pitri',N'Matri Pitri',NULL),
   ('LIFEAREA_PARENTS','en','Latn',N'Parents',NULL,N'Everything related to parents, and to the blood-relatives of parents (PVR Table 11, D-12).'),
   ('LIFEAREA_VEHICLES_COMFORTS','sa','Latn',N'Vahana Sukha',N'Vahana Sukha',NULL),
   ('LIFEAREA_VEHICLES_COMFORTS','en','Latn',N'Vehicles and comforts',NULL,N'Vehicles, pleasures, comforts and discomforts (PVR Table 11, D-16).'),
   ('LIFEAREA_RELIGION_SPIRITUALITY','sa','Latn',N'Dharma Adhyatma',N'Dharma Adhyatma',NULL),
   ('LIFEAREA_RELIGION_SPIRITUALITY','en','Latn',N'Religion and spirituality',NULL,N'Religious activities and spiritual matters (PVR Table 11, D-20).'),
   ('LIFEAREA_EDUCATION','sa','Latn',N'Vidya',N'Vidya',NULL),
   ('LIFEAREA_EDUCATION','en','Latn',N'Learning and education',NULL,N'Learning, knowledge and education (PVR Table 11, D-24).'),
   ('LIFEAREA_INNATE_NATURE','sa','Latn',N'Svabhava',N'Svabhava',NULL),
   ('LIFEAREA_INNATE_NATURE','en','Latn',N'Strengths and inherent nature',NULL,N'Strengths and weaknesses, inherent nature (PVR Table 11, D-27).'),
   ('LIFEAREA_EVILS_PUNISHMENT','sa','Latn',N'Dushkrita Danda',N'Dushkrita Danda',NULL),
   ('LIFEAREA_EVILS_PUNISHMENT','en','Latn',N'Evils and punishment',NULL,N'Evils and punishment, the sub-conscious self, some diseases (PVR Table 11, D-30).'),
   ('LIFEAREA_AUSPICIOUS_EVENTS','sa','Latn',N'Shubha Ashubha',N'Shubha Ashubha',NULL),
   ('LIFEAREA_AUSPICIOUS_EVENTS','en','Latn',N'Auspicious and inauspicious events',NULL,N'Auspicious and inauspicious events (PVR Table 11, D-40).'),
   ('LIFEAREA_ALL_MATTERS','sa','Latn',N'Sarva Vishaya',N'Sarva Vishaya',NULL),
   ('LIFEAREA_ALL_MATTERS','en','Latn',N'All matters',NULL,N'All matters (PVR Table 11, D-45).'),
   ('LIFEAREA_PAST_LIFE_KARMA','sa','Latn',N'Prarabdha Karma',N'Prarabdha Karma',NULL),
   ('LIFEAREA_PAST_LIFE_KARMA','en','Latn',N'Past-life karma',NULL,N'Karma of past life; all matters (PVR Table 11, D-60).'),
   ('PLANE_PHYSICAL','sa','Latn',N'Sthula',N'Sthula',NULL),
   ('PLANE_PHYSICAL','en','Latn',N'Physical plane',NULL,N'PVR sec 6.4: divisional charts D-1..D-12 - body, wealth, residence, family and other physical-self matters.'),
   ('PLANE_MENTAL','sa','Latn',N'Manas',N'Manas',NULL),
   ('PLANE_MENTAL','en','Latn',N'Mental plane',NULL,N'PVR sec 6.4: divisional charts D-16, D-20, D-24 - pleasure, unhappiness, religiousness, learning.'),
   ('PLANE_SUBCONSCIOUS','sa','Latn',N'Adhomanas',N'Adhomanas',NULL),
   ('PLANE_SUBCONSCIOUS','en','Latn',N'Sub-conscious plane',NULL,N'PVR sec 6.4: divisional charts D-27, D-30 - inherent nature, strengths, weaknesses, psychological imbalances.'),
   ('PLANE_KARMIC','sa','Latn',N'Karmika',N'Karmika',NULL),
   ('PLANE_KARMIC','en','Latn',N'Karmic plane',NULL,N'PVR sec 6.4: divisional charts D-40, D-45, D-60 - existence shaped by the karma of previous lives.')
  ) AS v (Code, LanguageCode, Script, Name, TraditionalName, ShortDescription)
  JOIN dbo.tbl_Astro_Terminology t ON t.Code = v.Code
) AS src
ON tgt.TerminologyId = src.TerminologyId AND tgt.LanguageCode = src.LanguageCode AND tgt.Script = src.Script
WHEN MATCHED THEN UPDATE SET Name = src.Name, TraditionalName = src.TraditionalName, ShortDescription = src.ShortDescription
WHEN NOT MATCHED THEN INSERT (TerminologyId, LanguageCode, Script, Name, TraditionalName, ShortDescription)
    VALUES (src.TerminologyId, src.LanguageCode, src.Script, src.Name, src.TraditionalName, src.ShortDescription);
GO

-- --- Batch 6: ledger + summary ---
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '30_life_area_taxonomy.sql',
       'tbl_Dim_LifeArea(20, PVR Table 11); ChartType += PrimaryLifeAreaId; SpecialLagnas LifeAreaFocus->LifeAreaId + GL varga D10->D5; taxonomy += 24 concepts / 48 sa-en text rows.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '30_life_area_taxonomy.sql');
GO

DECLARE @areas    INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_LifeArea);
DECLARE @ctmiss   INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_ChartType WHERE PrimaryLifeAreaId IS NULL);
DECLARE @slmiss   INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_SpecialLagnas WHERE UsedInBook = 1 AND LifeAreaId IS NULL);
DECLARE @glvarga  INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_SpecialLagnas l
    JOIN dbo.tbl_Dim_ChartType c ON c.Id = l.RelatedVargaChartId
    WHERE l.LagnaCode = 'GHATI_LAGNA' AND c.Code = 'D5');
DECLARE @oldcol   INT = (SELECT CASE WHEN COL_LENGTH('dbo.tbl_Dim_SpecialLagnas','LifeAreaFocus') IS NULL THEN 0 ELSE 1 END);
DECLARE @concepts INT = (SELECT COUNT(*) FROM dbo.tbl_Astro_Terminology WHERE Category = 'LifeArea' OR Code LIKE 'PLANE[_]%');
DECLARE @notext   INT = (SELECT COUNT(*) FROM dbo.tbl_Astro_Terminology t
    WHERE (t.Category = 'LifeArea' OR t.Code LIKE 'PLANE[_]%')
      AND (NOT EXISTS (SELECT 1 FROM dbo.tbl_Astro_TerminologyText x WHERE x.TerminologyId = t.TerminologyId AND x.LanguageCode = 'sa')
        OR NOT EXISTS (SELECT 1 FROM dbo.tbl_Astro_TerminologyText x WHERE x.TerminologyId = t.TerminologyId AND x.LanguageCode = 'en')));
PRINT '30 applied: ' + CAST(@areas AS VARCHAR(10)) + ' life areas (expect 20), '
    + CAST(@ctmiss AS VARCHAR(10)) + ' chart types w/o a life area (expect 0), '
    + CAST(@slmiss AS VARCHAR(10)) + ' in-book special lagnas w/o a life area (expect 0), '
    + CAST(@glvarga AS VARCHAR(10)) + ' GL->D5 (expect 1), '
    + CAST(@oldcol AS VARCHAR(10)) + ' LifeAreaFocus column still present (expect 0), '
    + CAST(@concepts AS VARCHAR(10)) + ' taxonomy concepts (expect 24), '
    + CAST(@notext AS VARCHAR(10)) + ' concepts missing sa or en text (expect 0).';
GO
