-- =====================================================================
-- 32 - House reference points (PVR ch. 7, sec 7.3).
--
-- "Revisit the lagna" - the app computes HouseNumberFromLagna / FromSun /
-- FromMoon on tbl_Chart_KeyDetails but nothing describes what those
-- reference points ARE, and the other sec 7.3 references (Paaka,
-- Arudha, Karakamsa, Ghati, Hora, Sree, graha lagnas) are not modelled
-- at all.
--
--   tbl_Dim_HouseReference        - 17 rows, one per point houses can be
--                                   counted from: Lagna, Chandra Lagna,
--                                   Ravi Lagna, Paaka Lagna, Arudha
--                                   Lagna, Karakamsa Lagna, Ghati / Hora
--                                   / Bhaava / Sree Lagna, + the seven
--                                   graha lagnas. Each carries its
--                                   "perspective" (sec 7.3 text), the
--                                   underlying-point basis (planet /
--                                   special lagna / lagna lord / arudha /
--                                   karaka-in-varga) and the Saturn-
--                                   transit note where the book gives one.
--   tbl_Rule_HouseReferenceMatter - PVR Table 12: which houses each
--                                   planetary reference (graha lagna) is
--                                   classically read for. RuleSetId 1.
--   tbl_Fact_HouseFromReference   - EMPTY. Narrow star-schema target for
--                                   per-chart "house of <subject> from
--                                   <reference>" - additive: a new
--                                   reference needs only a tbl_Dim_House-
--                                   Reference row, no schema change. The
--                                   engine that fills it is a later C#
--                                   follow-on; tbl_Chart_KeyDetails
--                                   .HouseNumberFrom{Lagna,Sun,Moon} stay
--                                   as-is (the common read path).
--   tbl_Rule_HouseAttribute       += NATURAL_SIGNIFICATOR rows (14) - the
--                                   house -> naisargika karaka view of
--                                   Table 12, promised by migration 31's
--                                   catalogue row.
--   tbl_Astro_Terminology         += Category 'HouseReference'; 6 named
--                                   HREF_* concepts + HREF_GRAHA_LAGNA,
--                                   sa/en (addendum). Ghati / Hora /
--                                   Bhaava / Sree reuse the SPT_* concepts
--                                   from migration 29.
--
-- Idempotent throughout. Apply:
--   sqlcmd -S localhost -E -d ikiastrro -b -i db/32_add_house_reference_points.sql
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- --- Batch 1: tbl_Dim_HouseReference ---
IF OBJECT_ID('dbo.tbl_Dim_HouseReference', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_HouseReference (
        ReferenceCode       VARCHAR(24)   NOT NULL
                                CONSTRAINT PK_Dim_HouseReference PRIMARY KEY,
        ReferenceName       NVARCHAR(40)  NOT NULL,
        BasisKind           VARCHAR(16)   NOT NULL,
        BasisPlanetId       TINYINT       NULL
                                CONSTRAINT FK_Dim_HouseReference_Planet FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        BasisSpecialLagnaId TINYINT       NULL
                                CONSTRAINT FK_Dim_HouseReference_SpecialLagna FOREIGN KEY REFERENCES dbo.tbl_Dim_SpecialLagnas (Id),
        Perspective         NVARCHAR(300) NOT NULL,                     -- PVR sec 7.3 characteristic
        AppliesInVarga      VARCHAR(12)   NOT NULL CONSTRAINT DF_Dim_HouseReference_Varga DEFAULT 'Any',
        SaturnTransitEffect NVARCHAR(200) NULL,                         -- PVR sec 7.3 where given
        IsDefault           BIT           NOT NULL CONSTRAINT DF_Dim_HouseReference_IsDefault DEFAULT 0,
        SortOrder           TINYINT       NOT NULL,
        IsActive            BIT           NOT NULL CONSTRAINT DF_Dim_HouseReference_IsActive DEFAULT 1,
        SourceRefCode       VARCHAR(40)   NULL,
        CONSTRAINT CK_Dim_HouseReference_Basis CHECK (BasisKind IN ('Ascendant','Planet','SpecialLagna','LagnaLord','Arudha','KarakaInVarga')),
        CONSTRAINT CK_Dim_HouseReference_Varga CHECK (AppliesInVarga IN ('Any','Navamsa','Rasi')),
        CONSTRAINT CK_Dim_HouseReference_Src   CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%')
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_HouseReference)
    INSERT dbo.tbl_Dim_HouseReference
        (ReferenceCode, ReferenceName, BasisKind, BasisPlanetId, BasisSpecialLagnaId,
         Perspective, AppliesInVarga, SaturnTransitEffect, IsDefault, SortOrder, SourceRefCode)
    SELECT v.ReferenceCode, v.ReferenceName, v.BasisKind, p.Id, sl.Id,
           v.Perspective, v.AppliesInVarga, v.SaturnTransitEffect, v.IsDefault, v.SortOrder, 'SRC_PVR_INTEGRATED'
    FROM (VALUES
    ('LAGNA',           N'Lagna',               'Ascendant',    CONVERT(VARCHAR(15),NULL), CONVERT(VARCHAR(20),NULL),
        N'The true self - the "spirit of I". The default reference; houses counted from here describe the whole of life.',
        'Any', N'Saturn transiting the lagna sign brings obstructions, delays and physical hardship (part of Sade Sati).', 1, 1),
    ('CHANDRA_LAGNA',   N'Chandra Lagna (Moon)','Planet',       'Moon', NULL,
        N'The standpoint of the mind (the Moon signifies the mind). Read for happiness, mental peace, ambition and one''s own view of career. PVR: it should not be ignored.',
        'Any', N'Saturn transiting the Moon''s sign is Sade Sati proper - mental pressure, frustration and low mood.', 0, 2),
    ('RAVI_LAGNA',      N'Ravi Lagna (Sun)',    'Planet',       'Sun', NULL,
        N'The standpoint of the soul (the Sun signifies the self) and of physical vitality.',
        'Any', NULL, 0, 3),
    ('ARUDHA_LAGNA',    N'Arudha Lagna',        'Arudha',       NULL, NULL,
        N'How the native is perceived in the world - image, reputation and material status.',
        'Any', NULL, 0, 4),
    ('PAAKA_LAGNA',     N'Paaka Lagna',         'LagnaLord',    NULL, NULL,
        N'The physical self of the native - the sign occupied by the lord of the lagna.',
        'Any', N'Saturn transiting the Paaka lagna can bring sickness and loss of vitality (PVR sec 7.3.5).', 0, 5),
    ('KARAKAMSA_LAGNA', N'Karakamsa Lagna',     'KarakaInVarga',NULL, NULL,
        N'The inner self - the sign occupied by the Atma Karaka in the navamsa (D-9). The 12th from it shows liberation (moksha).',
        'Navamsa', NULL, 0, 6),
    ('GHATI_LAGNA',     N'Ghati Lagna',         'SpecialLagna', NULL, 'GHATI_LAGNA',
        N'Power, authority and fame - weighed for promotions and political or public life.',
        'Any', NULL, 0, 7),
    ('HORA_LAGNA',      N'Hora Lagna',          'SpecialLagna', NULL, 'HORA_LAGNA',
        N'Wealth and money.',
        'Any', NULL, 0, 8),
    ('BHAAVA_LAGNA',    N'Bhaava Lagna',        'SpecialLagna', NULL, 'BHAAVA_LAGNA',
        N'A general timing reference advancing one rasi per two hours from sunrise. Defined for completeness; not used in PVR''s Integrated Approach.',
        'Any', NULL, 0, 9),
    ('SREE_LAGNA',      N'Sree Lagna',          'SpecialLagna', NULL, 'SREE_LAGNA',
        N'Prosperity - the reference point from which Sudasa is reckoned.',
        'Rasi', NULL, 0, 10),
    ('GRAHA_LAGNA_SUN',     N'Graha Lagna - Sun',     'Planet', 'Sun',     NULL, N'Graha lagna: the Sun as reference for the houses it naturally signifies (PVR Table 12: 9, 10, 11).',        'Any', NULL, 0, 11),
    ('GRAHA_LAGNA_MOON',    N'Graha Lagna - Moon',    'Planet', 'Moon',    NULL, N'Graha lagna: the Moon as reference for the houses it naturally signifies (PVR Table 12: 4, 1, 2, 11, 9).',   'Any', NULL, 0, 12),
    ('GRAHA_LAGNA_MARS',    N'Graha Lagna - Mars',    'Planet', 'Mars',    NULL, N'Graha lagna: Mars as reference for the house it naturally signifies (PVR Table 12: 3).',                     'Any', NULL, 0, 13),
    ('GRAHA_LAGNA_MERCURY', N'Graha Lagna - Mercury', 'Planet', 'Mercury', NULL, N'Graha lagna: Mercury as reference for the house it naturally signifies (PVR Table 12: 6).',                  'Any', NULL, 0, 14),
    ('GRAHA_LAGNA_JUPITER', N'Graha Lagna - Jupiter', 'Planet', 'Jupiter', NULL, N'Graha lagna: Jupiter as reference for the house it naturally signifies (PVR Table 12: 5).',                  'Any', NULL, 0, 15),
    ('GRAHA_LAGNA_VENUS',   N'Graha Lagna - Venus',   'Planet', 'Venus',   NULL, N'Graha lagna: Venus as reference for the house it naturally signifies (PVR Table 12: 7).',                    'Any', NULL, 0, 16),
    ('GRAHA_LAGNA_SATURN',  N'Graha Lagna - Saturn',  'Planet', 'Saturn',  NULL, N'Graha lagna: Saturn as reference for the houses it naturally signifies (PVR Table 12: 8, 12).',              'Any', NULL, 0, 17)
    ) v (ReferenceCode, ReferenceName, BasisKind, PlanetName, LagnaCode, Perspective, AppliesInVarga, SaturnTransitEffect, IsDefault, SortOrder)
    LEFT JOIN dbo.tbl_Planets p            ON p.PlanetName = v.PlanetName
    LEFT JOIN dbo.tbl_Dim_SpecialLagnas sl ON sl.LagnaCode = v.LagnaCode;
GO

-- --- Batch 2: tbl_Rule_HouseReferenceMatter (PVR Table 12) ---
IF OBJECT_ID('dbo.tbl_Rule_HouseReferenceMatter', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_HouseReferenceMatter (
        Id            INT IDENTITY(1,1) NOT NULL
                          CONSTRAINT PK_Rule_HouseReferenceMatter PRIMARY KEY,
        RuleSetId     TINYINT      NOT NULL
                          CONSTRAINT FK_Rule_HouseRefMatter_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        ReferenceCode VARCHAR(24)  NOT NULL
                          CONSTRAINT FK_Rule_HouseRefMatter_Reference FOREIGN KEY REFERENCES dbo.tbl_Dim_HouseReference (ReferenceCode),
        HouseNumber   TINYINT      NOT NULL
                          CONSTRAINT FK_Rule_HouseRefMatter_House FOREIGN KEY REFERENCES dbo.tbl_Dim_House (HouseNumber),
        DisplayOrder  TINYINT      NOT NULL CONSTRAINT DF_Rule_HouseRefMatter_Order DEFAULT 1,
        Notes                NVARCHAR(200) NULL,
        MethodCode           VARCHAR(30)   NULL,
        RuleParametersJson   NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode        VARCHAR(40)   NULL,
        IsActive             BIT NOT NULL CONSTRAINT DF_Rule_HouseRefMatter_IsActive DEFAULT 1,
        CONSTRAINT CK_RuleHouseRefMatter_Json CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_RuleHouseRefMatter_Src  CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_HouseReferenceMatter UNIQUE (RuleSetId, ReferenceCode, HouseNumber)
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_HouseReferenceMatter)
    INSERT dbo.tbl_Rule_HouseReferenceMatter
        (RuleSetId, ReferenceCode, HouseNumber, DisplayOrder, MethodCode, SourceRefCode, IsActive)
    SELECT 1, v.ReferenceCode, v.HouseNumber, v.DisplayOrder, 'MAP_LOOKUP', 'SRC_PVR_INTEGRATED', 1
    FROM (VALUES
    ('GRAHA_LAGNA_SUN',     9,1),('GRAHA_LAGNA_SUN',    10,2),('GRAHA_LAGNA_SUN',    11,3),
    ('GRAHA_LAGNA_MOON',    4,1),('GRAHA_LAGNA_MOON',    1,2),('GRAHA_LAGNA_MOON',    2,3),('GRAHA_LAGNA_MOON',11,4),('GRAHA_LAGNA_MOON',9,5),
    ('GRAHA_LAGNA_MARS',    3,1),
    ('GRAHA_LAGNA_MERCURY', 6,1),
    ('GRAHA_LAGNA_JUPITER', 5,1),
    ('GRAHA_LAGNA_VENUS',   7,1),
    ('GRAHA_LAGNA_SATURN',  8,1),('GRAHA_LAGNA_SATURN', 12,2)
    ) v (ReferenceCode, HouseNumber, DisplayOrder);
GO

-- --- Batch 3: tbl_Fact_HouseFromReference (empty; engine is a later follow-on) ---
IF OBJECT_ID('dbo.tbl_Fact_HouseFromReference', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Fact_HouseFromReference (
        Id             INT IDENTITY(1,1) NOT NULL
                           CONSTRAINT PK_Fact_HouseFromReference PRIMARY KEY,
        ChartResultId  INT          NOT NULL
                           CONSTRAINT FK_Fact_HouseFromReference_ChartResult FOREIGN KEY REFERENCES dbo.tbl_ChartResults (Id),
        RuleSetId      TINYINT      NOT NULL
                           CONSTRAINT FK_Fact_HouseFromReference_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        ReferenceCode  VARCHAR(24)  NOT NULL
                           CONSTRAINT FK_Fact_HouseFromReference_Reference FOREIGN KEY REFERENCES dbo.tbl_Dim_HouseReference (ReferenceCode),
        SubjectKind    VARCHAR(12)  NOT NULL,                          -- Graha / SpecialLagna / Arudha / Lagna / Reference
        SubjectKey     VARCHAR(24)  NOT NULL,                          -- 'Sun'..'Ketu' / 'AL' / 'A7' / 'HL' / the ReferenceCode
        SubjectPlanetId TINYINT     NULL
                           CONSTRAINT FK_Fact_HouseFromReference_Planet FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        HouseNumber    TINYINT      NOT NULL,
        SignId         TINYINT      NULL
                           CONSTRAINT FK_Fact_HouseFromReference_Sign FOREIGN KEY REFERENCES dbo.tbl_SignAttributes (Id),
        ChartTypeId    TINYINT      NULL
                           CONSTRAINT FK_Fact_HouseFromReference_ChartType FOREIGN KEY REFERENCES dbo.tbl_Dim_ChartType (Id),
        CONSTRAINT CK_Fact_HouseFromReference_House CHECK (HouseNumber BETWEEN 1 AND 12),
        CONSTRAINT CK_Fact_HouseFromReference_Kind  CHECK (SubjectKind IN ('Graha','SpecialLagna','Arudha','Lagna','Reference')),
        CONSTRAINT UQ_Fact_HouseFromReference UNIQUE (ChartResultId, ChartTypeId, ReferenceCode, SubjectKind, SubjectKey)
    );
    CREATE NONCLUSTERED INDEX IX_Fact_HouseFromReference_ChartResultId ON dbo.tbl_Fact_HouseFromReference (ChartResultId);
END
GO

-- --- Batch 4: tbl_Rule_HouseAttribute NATURAL_SIGNIFICATOR (house -> Table 12 karaka) ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_HouseAttribute WHERE AttributeCode = 'NATURAL_SIGNIFICATOR')
    INSERT dbo.tbl_Rule_HouseAttribute
        (RuleSetId, HouseNumber, AttributeCode, ValueCode, ValueText, Priority, SourceRefCode, IsActive, Notes)
    SELECT 1, v.HouseNumber, 'NATURAL_SIGNIFICATOR', v.PlanetName, v.PlanetName, v.Priority,
           'SRC_PVR_INTEGRATED', 1, N'PVR Table 12 (sec 7.3.9), house -> graha view.'
    FROM (VALUES
    ( 1,'Moon',    1),
    ( 2,'Moon',    1),
    ( 3,'Mars',    1),
    ( 4,'Moon',    1),
    ( 5,'Jupiter', 1),
    ( 6,'Mercury', 1),
    ( 7,'Venus',   1),
    ( 8,'Saturn',  1),
    ( 9,'Sun',     1),
    ( 9,'Moon',    2),
    (10,'Sun',     1),
    (11,'Sun',     1),
    (11,'Moon',    2),
    (12,'Saturn',  1)
    ) v (HouseNumber, PlanetName, Priority);
GO

-- --- Batch 5: tbl_Rule_Catalog row ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_HouseReferenceMatter')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_HouseReferenceMatter', 'HOUSE', 'MAP_LOOKUP',
            'PVR Table 12 (sec 7.3.9): the houses each planetary reference (graha lagna) is classically read for - Sun 9/10/11, Moon 4/1/2/11/9, Mars 3, Mercury 6, Jupiter 5, Venus 7, Saturn 8/12. One row per (rule-set, reference, house).',
            '32_add_house_reference_points.sql');
GO

-- --- Batch 6: taxonomy (addendum - not yet emitted by TerminologySeed.cs) ---
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Astro_Terminology_Category')
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints
                   WHERE name = 'CK_Astro_Terminology_Category' AND definition LIKE '%HouseReference%')
BEGIN
    ALTER TABLE dbo.tbl_Astro_Terminology DROP CONSTRAINT CK_Astro_Terminology_Category;
    ALTER TABLE dbo.tbl_Astro_Terminology ADD CONSTRAINT CK_Astro_Terminology_Category CHECK (Category IN (
        'Planet','Sign','House','Nakshatra','NakshatraPada','DivisionalChart','Karaka',
        'SpecialPoint','AvasthaState','DignityState','Relationship','StrengthComponent',
        'Dasha','Yoga','Ayanamsa','Concept','LifeArea','HouseCategory','HouseReference'));
END
GO
MERGE dbo.tbl_Astro_Terminology AS tgt
USING (VALUES
    ('HouseReference','HREF_LAGNA',            CONVERT(VARCHAR(40),NULL),'HOUSE',CONVERT(INT,NULL),990),
    ('HouseReference','HREF_CHANDRA_LAGNA',    NULL,'HOUSE',NULL,991),
    ('HouseReference','HREF_RAVI_LAGNA',       NULL,'HOUSE',NULL,992),
    ('HouseReference','HREF_ARUDHA_LAGNA',     NULL,'HOUSE',NULL,993),
    ('HouseReference','HREF_PAAKA_LAGNA',      NULL,'HOUSE',NULL,994),
    ('HouseReference','HREF_KARAKAMSA_LAGNA',  NULL,'HOUSE',NULL,995),
    ('HouseReference','HREF_GRAHA_LAGNA',      NULL,'HOUSE',NULL,996)
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
   ('HREF_LAGNA','sa','Latn',N'Lagna',N'Lagna',NULL),
   ('HREF_LAGNA','en','Latn',N'Lagna',NULL,N'The ascendant used as the reference point for counting houses - the true self, the default reference (PVR sec 7.3.1).'),
   ('HREF_CHANDRA_LAGNA','sa','Latn',N'Chandra Lagna',N'Chandra Lagna',NULL),
   ('HREF_CHANDRA_LAGNA','en','Latn',N'Chandra Lagna',NULL,N'The Moon''s sign used as the reference point - houses then show matters from the standpoint of the mind (PVR sec 7.3.2).'),
   ('HREF_RAVI_LAGNA','sa','Latn',N'Ravi Lagna',N'Ravi Lagna',NULL),
   ('HREF_RAVI_LAGNA','en','Latn',N'Ravi Lagna',NULL,N'The Sun''s sign used as the reference point - houses then show matters from the standpoint of the soul and of physical vitality (PVR sec 7.3.3).'),
   ('HREF_ARUDHA_LAGNA','sa','Latn',N'Arudha Lagna',N'Arudha Lagna',NULL),
   ('HREF_ARUDHA_LAGNA','en','Latn',N'Arudha Lagna',NULL,N'The pada of the lagna used as the reference point - how the native is perceived in the world; image and material status (PVR sec 7.3.4).'),
   ('HREF_PAAKA_LAGNA','sa','Latn',N'Paaka Lagna',N'Paaka Lagna',NULL),
   ('HREF_PAAKA_LAGNA','en','Latn',N'Paaka Lagna',NULL,N'The sign occupied by the lord of the lagna, used as the reference point - the physical self of the native (PVR sec 7.3.5).'),
   ('HREF_KARAKAMSA_LAGNA','sa','Latn',N'Karakamsa Lagna',N'Karakamsa Lagna',NULL),
   ('HREF_KARAKAMSA_LAGNA','en','Latn',N'Karakamsa Lagna',NULL,N'The sign occupied by the Atma Karaka in the navamsa, used as the reference point - the inner self; the 12th from it shows moksha (PVR sec 7.3.6).'),
   ('HREF_GRAHA_LAGNA','sa','Latn',N'Graha Lagna',N'Graha Lagna',NULL),
   ('HREF_GRAHA_LAGNA','en','Latn',N'Graha Lagna',NULL,N'A planet used as the reference point for the houses it naturally signifies (PVR sec 7.3.9, Table 12).')
  ) AS v (Code, LanguageCode, Script, Name, TraditionalName, ShortDescription)
  JOIN dbo.tbl_Astro_Terminology t ON t.Code = v.Code
) AS src
ON tgt.TerminologyId = src.TerminologyId AND tgt.LanguageCode = src.LanguageCode AND tgt.Script = src.Script
WHEN MATCHED THEN UPDATE SET Name = src.Name, TraditionalName = src.TraditionalName, ShortDescription = src.ShortDescription
WHEN NOT MATCHED THEN INSERT (TerminologyId, LanguageCode, Script, Name, TraditionalName, ShortDescription)
    VALUES (src.TerminologyId, src.LanguageCode, src.Script, src.Name, src.TraditionalName, src.ShortDescription);
GO

-- --- Batch 7: ledger + summary ---
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '32_add_house_reference_points.sql',
       'tbl_Dim_HouseReference(17); tbl_Rule_HouseReferenceMatter(14=Table 12); empty tbl_Fact_HouseFromReference; +14 NATURAL_SIGNIFICATOR; taxonomy +7 concepts/14 text.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '32_add_house_reference_points.sql');
GO

DECLARE @refs   INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_HouseReference);
DECLARE @refpl  INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_HouseReference WHERE BasisKind = 'Planet' AND BasisPlanetId IS NULL);
DECLARE @refsl  INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_HouseReference WHERE BasisKind = 'SpecialLagna' AND BasisSpecialLagnaId IS NULL);
DECLARE @t12    INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_HouseReferenceMatter);
DECLARE @fact   INT = (SELECT CASE WHEN OBJECT_ID('dbo.tbl_Fact_HouseFromReference') IS NULL THEN 0 ELSE 1 END);
DECLARE @natsig INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_HouseAttribute WHERE AttributeCode = 'NATURAL_SIGNIFICATOR');
DECLARE @cat    INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_HouseReferenceMatter');
DECLARE @concepts INT = (SELECT COUNT(*) FROM dbo.tbl_Astro_Terminology WHERE Category = 'HouseReference');
DECLARE @notext INT = (SELECT COUNT(*) FROM dbo.tbl_Astro_Terminology t
    WHERE t.Category = 'HouseReference'
      AND (NOT EXISTS (SELECT 1 FROM dbo.tbl_Astro_TerminologyText x WHERE x.TerminologyId = t.TerminologyId AND x.LanguageCode = 'sa')
        OR NOT EXISTS (SELECT 1 FROM dbo.tbl_Astro_TerminologyText x WHERE x.TerminologyId = t.TerminologyId AND x.LanguageCode = 'en')));
DECLARE @orphan INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_HouseReferenceMatter r
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_HouseReference d WHERE d.ReferenceCode = r.ReferenceCode));
PRINT '32 applied: ' + CAST(@refs AS VARCHAR(10)) + ' references (expect 17), '
    + CAST(@refpl AS VARCHAR(10)) + ' planet-basis w/o planet id (expect 0), '
    + CAST(@refsl AS VARCHAR(10)) + ' lagna-basis w/o special-lagna id (expect 0), '
    + CAST(@t12 AS VARCHAR(10)) + ' Table 12 rows (expect 14), '
    + CAST(@fact AS VARCHAR(10)) + ' fact table present (expect 1), '
    + CAST(@natsig AS VARCHAR(10)) + ' NATURAL_SIGNIFICATOR rows (expect 14), '
    + CAST(@orphan AS VARCHAR(10)) + ' orphan Table-12 refs (expect 0), '
    + CAST(@cat AS VARCHAR(10)) + ' rule-catalog row (expect 1), '
    + CAST(@concepts AS VARCHAR(10)) + ' taxonomy concepts (expect 7), '
    + CAST(@notext AS VARCHAR(10)) + ' concepts missing sa or en text (expect 0).';
GO
