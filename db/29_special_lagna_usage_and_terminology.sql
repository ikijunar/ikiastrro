-- =====================================================================
-- 29 - Special Lagna: usage / varga correlation columns + terminology.
--
-- Two parts, both DB-only (PVR "Integrated Approach" ch 5 / sec 5.6 /
-- sec 6.3 / sec 7.1):
--
--  (A) tbl_Dim_SpecialLagnas gains five columns that say WHEN each
--      special lagna is brought in and WHICH divisional chart it pairs
--      with:
--        LifeAreaFocus        - Wealth | PowerAndFame | Prosperity (NULL
--                               for Bhaava - unused).
--        UsageContext         - the sec 5.6 timing guidance in prose.
--        HouseReferenceScope  - AnyChart | RasiChartOnly | NotUsed. sec 7.1
--                               uses Ghati Lagna as a house reference in
--                               any chart; Sree Lagna's documented use is
--                               the rasi chart (Sudasa) only; Bhaava is
--                               not used.
--        DasaLinkage          - 'Sudasa' for Sree Lagna, else NULL.
--        RelatedVargaChartId  - a SOFT interpretive pairing (PVR sec 6.3:
--                               each varga lights one sphere of life) -
--                               Hora Lagna <-> D2 (Hora, wealth), Ghati
--                               Lagna <-> D10 (Dasamsa, power/status).
--                               NOT a chapter-5 rule; NULL where none
--                               applies. FK tbl_Dim_ChartType.
--
--  (B) the bilingual taxonomy (tbl_Astro_Terminology / _Text) gains the
--      special-lagna vocabulary so later ta / Deva translations have a
--      reference row: SPT_BL / SPT_HL / SPT_GL / SPT_SL (SpecialPoint) and
--      the calculation-family / anchor / basis concepts (Concept). Seeded
--      as a hand-maintained ADDENDUM after the generated TERMINOLOGY SEED
--      block - TerminologySeed.cs does not yet know these codes; add them
--      there on the next generator pass to keep it authoritative.
--
-- Idempotent: column adds guarded by COL_LENGTH; terminology via MERGE.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/29_special_lagna_usage_and_terminology.sql
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- --- Batch 1: tbl_Dim_SpecialLagnas usage / varga columns ---
IF COL_LENGTH('dbo.tbl_Dim_SpecialLagnas', 'LifeAreaFocus') IS NULL
    ALTER TABLE dbo.tbl_Dim_SpecialLagnas ADD LifeAreaFocus VARCHAR(24) NULL;   -- Wealth | PowerAndFame | Prosperity ; NULL = none
GO
IF COL_LENGTH('dbo.tbl_Dim_SpecialLagnas', 'UsageContext') IS NULL
    ALTER TABLE dbo.tbl_Dim_SpecialLagnas ADD UsageContext NVARCHAR(400) NULL;   -- PVR sec 5.6: when to bring this lagna in
GO
IF COL_LENGTH('dbo.tbl_Dim_SpecialLagnas', 'HouseReferenceScope') IS NULL
    ALTER TABLE dbo.tbl_Dim_SpecialLagnas ADD HouseReferenceScope VARCHAR(20)
        NOT NULL CONSTRAINT DF_Dim_SpecialLagnas_HouseRefScope DEFAULT 'AnyChart';
GO
IF COL_LENGTH('dbo.tbl_Dim_SpecialLagnas', 'DasaLinkage') IS NULL
    ALTER TABLE dbo.tbl_Dim_SpecialLagnas ADD DasaLinkage VARCHAR(30) NULL;       -- 'Sudasa' for Sree Lagna
GO
IF COL_LENGTH('dbo.tbl_Dim_SpecialLagnas', 'RelatedVargaChartId') IS NULL
    ALTER TABLE dbo.tbl_Dim_SpecialLagnas ADD RelatedVargaChartId TINYINT NULL   -- soft PVR sec 6.3 pairing, not a ch-5 rule
        CONSTRAINT FK_Dim_SpecialLagnas_RelatedVarga FOREIGN KEY REFERENCES dbo.tbl_Dim_ChartType (Id);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Dim_SpecialLagnas_HouseRefScope')
    ALTER TABLE dbo.tbl_Dim_SpecialLagnas ADD CONSTRAINT CK_Dim_SpecialLagnas_HouseRefScope
        CHECK (HouseReferenceScope IN ('AnyChart','RasiChartOnly','NotUsed'));
GO

-- --- Batch 2: backfill the 4 rows ---
UPDATE dbo.tbl_Dim_SpecialLagnas
   SET LifeAreaFocus = NULL, HouseReferenceScope = 'NotUsed', DasaLinkage = NULL, RelatedVargaChartId = NULL,
       UsageContext = N'PVR does not use Bhaava Lagna in the Integrated Approach; it is defined only for completeness (sec 5.2).'
 WHERE LagnaCode = 'BHAAVA_LAGNA';

UPDATE dbo.tbl_Dim_SpecialLagnas
   SET LifeAreaFocus = 'Wealth', HouseReferenceScope = 'AnyChart', DasaLinkage = NULL,
       RelatedVargaChartId = (SELECT Id FROM dbo.tbl_Dim_ChartType WHERE Code = 'D2'),
       UsageContext = N'Bring in when timing wealth / money / prosperity periods - PVR sec 5.6 weighs it heavily for someone whose life runs on business or trade. Read houses from HL and cross-check the D2 (Hora) chart.'
 WHERE LagnaCode = 'HORA_LAGNA';

UPDATE dbo.tbl_Dim_SpecialLagnas
   SET LifeAreaFocus = 'PowerAndFame', HouseReferenceScope = 'AnyChart', DasaLinkage = NULL,
       RelatedVargaChartId = (SELECT Id FROM dbo.tbl_Dim_ChartType WHERE Code = 'D10'),
       UsageContext = N'Bring in when timing fame / power / authority periods - PVR sec 5.6 weighs it heavily for someone in politics or public office. Read houses from GL and cross-check the D10 (Dasamsa) chart. sec 5.5: GL shifts 1 deg 15 min per birthtime minute, so correct the birthtime before trusting it in vargas.'
 WHERE LagnaCode = 'GHATI_LAGNA';

UPDATE dbo.tbl_Dim_SpecialLagnas
   SET LifeAreaFocus = 'Prosperity', HouseReferenceScope = 'RasiChartOnly', DasaLinkage = 'Sudasa', RelatedVargaChartId = NULL,
       UsageContext = N'The seed point for Sudasa ("Sree Lagna Kendradi Rasi Dasa") - dasas start from the sign holding Sree Lagna (PVR sec 5.7 and the Sudasa chapter). Not read as a chart of its own.'
 WHERE LagnaCode = 'SREE_LAGNA';
GO

-- --- Batch 3: terminology ADDENDUM - concepts ---
-- Hand-maintained; NOT emitted by TerminologySeed.cs yet. Same MERGE
-- shape as the generated TERMINOLOGY SEED block. SpecialPoint rows keep
-- EngineCode 'KARAKA' to match the sibling SPT_* rows; the calculation
-- vocabulary is EngineCode 'SPECIALLAGNA' (the tbl_Rule_Catalog code).
MERGE dbo.tbl_Astro_Terminology AS tgt
USING (VALUES
  ('SpecialPoint','SPT_BL',NULL,'KARAKA',NULL,901),
  ('SpecialPoint','SPT_HL',NULL,'KARAKA',NULL,902),
  ('SpecialPoint','SPT_GL',NULL,'KARAKA',NULL,903),
  ('SpecialPoint','SPT_SL',NULL,'KARAKA',NULL,904),
  ('Concept','CALC_TIME_FROM_SUNRISE',NULL,'SPECIALLAGNA',NULL,905),
  ('Concept','CALC_NAKSHATRA_FRACTION',NULL,'SPECIALLAGNA',NULL,906),
  ('Concept','ANCHOR_SUN_AT_SUNRISE',NULL,'SPECIALLAGNA',NULL,907),
  ('Concept','ANCHOR_NATAL_LAGNA',NULL,'SPECIALLAGNA',NULL,908),
  ('Concept','BASIS_NAKSHATRA_SPAN',NULL,'SPECIALLAGNA',NULL,909)
) AS src (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder)
ON tgt.Code = src.Code
WHEN MATCHED THEN UPDATE SET Category = src.Category, ParentCode = src.ParentCode,
    EngineCode = src.EngineCode, NumericKey = src.NumericKey, DisplayOrder = src.DisplayOrder, IsActive = 1
WHEN NOT MATCHED THEN INSERT (Category, Code, ParentCode, EngineCode, NumericKey, DisplayOrder, IsActive)
    VALUES (src.Category, src.Code, src.ParentCode, src.EngineCode, src.NumericKey, src.DisplayOrder, 1);
GO

-- --- Batch 4: terminology ADDENDUM - sa + en text ---
MERGE dbo.tbl_Astro_TerminologyText AS tgt
USING (
  SELECT t.TerminologyId, v.LanguageCode, v.Script, v.Name, v.TraditionalName, v.ShortDescription
  FROM (VALUES
   ('SPT_BL','sa','Latn',N'Bhaava Lagna',N'Bhaava Lagna',NULL),
   ('SPT_BL','en','Latn',N'Bhaava Lagna',NULL,N'Special lagna at the Sun''s sunrise longitude, advancing one sign per two hours. Defined for completeness; not used in PVR''s Integrated Approach.'),
   ('SPT_HL','sa','Latn',N'Hora Lagna',N'Hora Lagna',NULL),
   ('SPT_HL','en','Latn',N'Hora Lagna',NULL,N'Special lagna advancing one sign per hora (hour) from the Sun''s sunrise longitude. Shows the self with respect to wealth and money; weighed when timing prosperity periods (PVR sec 5.3 / 5.6).'),
   ('SPT_GL','sa','Latn',N'Ghati Lagna',N'Ghati Lagna',NULL),
   ('SPT_GL','en','Latn',N'Ghati Lagna',NULL,N'Special lagna advancing one sign per ghati (24 minutes) from the Sun''s sunrise longitude; also called Ghatika Lagna. Shows the self with respect to power, authority and fame; weighed when timing public-life periods (PVR sec 5.4 / 5.6).'),
   ('SPT_SL','sa','Latn',N'Sree Lagna',N'Sree Lagna',NULL),
   ('SPT_SL','en','Latn',N'Sree Lagna',NULL,N'The natal lagna plus the Moon''s fraction through its nakshatra scaled to the whole zodiac. Signifies prosperity; the reference point from which Sudasa is reckoned (PVR sec 5.7).'),
   ('CALC_TIME_FROM_SUNRISE','sa','Latn',N'Time from sunrise',N'Time from sunrise',NULL),
   ('CALC_TIME_FROM_SUNRISE','en','Latn',N'Time from sunrise',NULL,N'Special-lagna calculation family: a fixed angular rate per minute elapsed since the day''s opening sunrise, added to the Sun''s sunrise longitude (Bhaava, Hora, Ghati).'),
   ('CALC_NAKSHATRA_FRACTION','sa','Latn',N'Nakshatra fraction',N'Nakshatra fraction',NULL),
   ('CALC_NAKSHATRA_FRACTION','en','Latn',N'Nakshatra fraction',NULL,N'Special-lagna calculation family: the Moon''s fractional progress through its nakshatra, scaled to 360 degrees and added to the natal lagna (Sree Lagna).'),
   ('ANCHOR_SUN_AT_SUNRISE','sa','Latn',N'Sun at sunrise',N'Sun at sunrise',NULL),
   ('ANCHOR_SUN_AT_SUNRISE','en','Latn',N'Sun at sunrise',NULL,N'Base longitude for the time-rate special lagnas: the Sun''s nirayana longitude at the day''s opening sunrise.'),
   ('ANCHOR_NATAL_LAGNA','sa','Latn',N'Natal lagna',N'Natal lagna',NULL),
   ('ANCHOR_NATAL_LAGNA','en','Latn',N'Natal lagna',NULL,N'Base longitude for Sree Lagna: the ascendant of the birth (rasi) chart.'),
   ('BASIS_NAKSHATRA_SPAN','sa','Latn',N'Nakshatra span',N'Nakshatra span',NULL),
   ('BASIS_NAKSHATRA_SPAN','en','Latn',N'Nakshatra span',NULL,N'The 13 degrees 20 minutes extent of one nakshatra - the denominator when taking the Moon''s fractional progress for Sree Lagna.')
  ) AS v (Code, LanguageCode, Script, Name, TraditionalName, ShortDescription)
  JOIN dbo.tbl_Astro_Terminology t ON t.Code = v.Code
) AS src
ON tgt.TerminologyId = src.TerminologyId AND tgt.LanguageCode = src.LanguageCode AND tgt.Script = src.Script
WHEN MATCHED THEN UPDATE SET Name = src.Name, TraditionalName = src.TraditionalName, ShortDescription = src.ShortDescription
WHEN NOT MATCHED THEN INSERT (TerminologyId, LanguageCode, Script, Name, TraditionalName, ShortDescription)
    VALUES (src.TerminologyId, src.LanguageCode, src.Script, src.Name, src.TraditionalName, src.ShortDescription);
GO

-- --- Batch 5: ledger + summary ---
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '29_special_lagna_usage_and_terminology.sql',
       'tbl_Dim_SpecialLagnas += 5 usage/varga columns, 4 rows backfilled; terminology addendum 9 concepts + 18 sa/en text rows (SPT_BL/HL/GL/SL + calc vocabulary).'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '29_special_lagna_usage_and_terminology.sql');
GO

DECLARE @cols     INT = (SELECT COUNT(*) FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.tbl_Dim_SpecialLagnas')
      AND name IN ('LifeAreaFocus','UsageContext','HouseReferenceScope','DasaLinkage','RelatedVargaChartId'));
DECLARE @nousage  INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_SpecialLagnas WHERE UsageContext IS NULL);
DECLARE @hlvarga  INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_SpecialLagnas l
    JOIN dbo.tbl_Dim_ChartType c ON c.Id = l.RelatedVargaChartId
    WHERE l.LagnaCode = 'HORA_LAGNA' AND c.Code = 'D2');
DECLARE @glvarga  INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_SpecialLagnas l
    JOIN dbo.tbl_Dim_ChartType c ON c.Id = l.RelatedVargaChartId
    WHERE l.LagnaCode = 'GHATI_LAGNA' AND c.Code = 'D10');
DECLARE @sudasa   INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_SpecialLagnas
    WHERE LagnaCode = 'SREE_LAGNA' AND DasaLinkage = 'Sudasa');
DECLARE @concepts INT = (SELECT COUNT(*) FROM dbo.tbl_Astro_Terminology
    WHERE Code IN ('SPT_BL','SPT_HL','SPT_GL','SPT_SL','CALC_TIME_FROM_SUNRISE','CALC_NAKSHATRA_FRACTION',
                   'ANCHOR_SUN_AT_SUNRISE','ANCHOR_NATAL_LAGNA','BASIS_NAKSHATRA_SPAN'));
DECLARE @notext   INT = (SELECT COUNT(*) FROM dbo.tbl_Astro_Terminology t
    WHERE t.Code IN ('SPT_BL','SPT_HL','SPT_GL','SPT_SL','CALC_TIME_FROM_SUNRISE','CALC_NAKSHATRA_FRACTION',
                     'ANCHOR_SUN_AT_SUNRISE','ANCHOR_NATAL_LAGNA','BASIS_NAKSHATRA_SPAN')
      AND (NOT EXISTS (SELECT 1 FROM dbo.tbl_Astro_TerminologyText x WHERE x.TerminologyId = t.TerminologyId AND x.LanguageCode = 'sa')
        OR NOT EXISTS (SELECT 1 FROM dbo.tbl_Astro_TerminologyText x WHERE x.TerminologyId = t.TerminologyId AND x.LanguageCode = 'en')));
PRINT '29 applied: ' + CAST(@cols AS VARCHAR(10)) + ' new columns (expect 5), '
    + CAST(@nousage AS VARCHAR(10)) + ' rows w/o UsageContext (expect 0), '
    + CAST(@hlvarga AS VARCHAR(10)) + ' HL->D2 (expect 1), '
    + CAST(@glvarga AS VARCHAR(10)) + ' GL->D10 (expect 1), '
    + CAST(@sudasa AS VARCHAR(10)) + ' SL->Sudasa (expect 1), '
    + CAST(@concepts AS VARCHAR(10)) + ' terminology concepts (expect 9), '
    + CAST(@notext AS VARCHAR(10)) + ' concepts missing sa or en text (expect 0).';
GO
