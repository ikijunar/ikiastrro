-- =====================================================================
-- 28 - Special Lagna reference layer: master + calculation rules.
--
-- PVR "Vedic Astrology: An Integrated Approach" chapter 5 (Special
-- Lagnas). English "Special Lagna" vocabulary (rammyps, 2026-09-04).
-- Names follow STANDARDS.md sec D: tbl_<Category>_<PascalCase> (no
-- internal underscore after the infix), Dim names plural, Rule names
-- singular, PK column is Id.
--
--   tbl_Dim_SpecialLagnas          - the 4-row master. Two calculation
--                                    families: TIME_FROM_SUNRISE (Bhaava,
--                                    Hora, Ghati - each a fixed degrees-
--                                    per-minute advance of the Sun's
--                                    sunrise longitude) and
--                                    NAKSHATRA_FRACTION (Sree - the natal
--                                    lagna plus Moon's fraction through
--                                    its nakshatra, scaled to 360 deg).
--   tbl_Rule_SpecialLagnaTimeRate  - Bhaava / Hora / Ghati Lagna: the
--                                    single DegreesPerMinute coefficient
--                                    and the anchor point. PVR sec 5.2 -
--                                    5.4.
--   tbl_Rule_SpecialLagnaFraction  - Sree Lagna: anchor = natal lagna,
--                                    the body whose nakshatra fraction is
--                                    taken (Moon), and the scale (360).
--                                    PVR sec 5.7.
--
-- Bhaava Lagna DegreesPerMinute is seeded 0.25 (15 deg/hour = one rasi
-- per 2 hours) per PVR sec 5.2's stated rate, the classical ishtakaala/5
-- rule and JHora / PyJHora. PVR sec 5.2's method step and worked Example 7
-- both take minutes-since-sunrise directly as degrees (1.0 deg/min), which
-- contradicts the same section's rate by a factor of 4 - treated as a book
-- erratum (rammyps, 2026-09-04). See the Bhaava row CalculationNarrative.
--
-- No varga projection here - D1 rule data only. RuleSetId 1
-- (Parashari-Classical), SourceRefCode SRC_PVR_INTEGRATED on every seeded
-- row - the one registered source for the P.V.R. Narasimha Rao corpus
-- (STANDARDS.md sec D / sec M.4).
--
-- Only Hora Lagna is built in C# today (HoraLagnaCalculator.cs); the
-- tbl_Rule_SpecialLagnaTimeRate Hora row records that shipped 0.5 value.
-- Bhaava / Ghati / Sree have no engine yet - reference data only.
--
-- Idempotent: table / catalog adds guarded; seeds are IF NOT EXISTS on
-- their table.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/28_add_special_lagna_rule_layer.sql
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- --- Batch 1: tbl_Dim_SpecialLagnas (master) ---
IF OBJECT_ID('dbo.tbl_Dim_SpecialLagnas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_SpecialLagnas (
        Id              TINYINT       NOT NULL
                            CONSTRAINT PK_Dim_SpecialLagnas PRIMARY KEY,     -- fixed 4-row domain, like tbl_Planets
        LagnaCode       VARCHAR(16)   NOT NULL
                            CONSTRAINT UQ_Dim_SpecialLagnas_Code UNIQUE,     -- BHAAVA_LAGNA, HORA_LAGNA, ...
        Abbreviation    VARCHAR(4)    NOT NULL
                            CONSTRAINT UQ_Dim_SpecialLagnas_Abbr UNIQUE,     -- BL, HL, GL, SL (== SpecialPointSeed.Code)
        LagnaName       VARCHAR(30)   NOT NULL
                            CONSTRAINT UQ_Dim_SpecialLagnas_Name UNIQUE,     -- app display name
        CalculationType VARCHAR(20)   NOT NULL,                              -- TIME_FROM_SUNRISE | NAKSHATRA_FRACTION
        UsedInBook      BIT           NOT NULL,                              -- 0 = PVR defines it "for completeness" only (Bhaava)
        Significations  NVARCHAR(300) NULL,                                  -- PVR sec 5.6
        SortOrder       TINYINT       NOT NULL,
        IsActive        BIT           NOT NULL CONSTRAINT DF_Dim_SpecialLagnas_IsActive DEFAULT 1,
        Notes           NVARCHAR(400) NULL,
        CONSTRAINT CK_Dim_SpecialLagnas_CalcType CHECK (CalculationType IN ('TIME_FROM_SUNRISE','NAKSHATRA_FRACTION'))
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Dim_SpecialLagnas)
    INSERT dbo.tbl_Dim_SpecialLagnas
        (Id, LagnaCode, Abbreviation, LagnaName, CalculationType, UsedInBook, Significations, SortOrder, Notes)
    VALUES
        (1, 'BHAAVA_LAGNA', 'BL', 'Bhaava Lagna', 'TIME_FROM_SUNRISE',  0,
            N'Defined for the sake of completeness; PVR does not use Bhaava Lagna further in the book.', 1,
            N'PVR sec 5.2 gives contradictory rates - see the tbl_Rule_SpecialLagnaTimeRate Bhaava row. Seeded per the stated "one rasi per 2 hours".'),
        (2, 'HORA_LAGNA',   'HL', 'Hora Lagna',   'TIME_FROM_SUNRISE',  1,
            N'Self with respect to money, wealth and prosperity; weighed heavily when timing periods for a businessman (PVR sec 5.6).', 2,
            N'The only special lagna built in C# today (HoraLagnaCalculator.cs).'),
        (3, 'GHATI_LAGNA',  'GL', 'Ghati Lagna',  'TIME_FROM_SUNRISE',  1,
            N'Self with respect to fame, power and authority; weighed heavily when timing periods for a politician (PVR sec 5.6). Also called Ghatika Lagna.', 3,
            N'PVR sec 5.5: a 1-minute birthtime error shifts GL by 1 deg 15 min, so GL is more birthtime-sensitive than the normal lagna, especially in vargas.'),
        (4, 'SREE_LAGNA',   'SL', 'Sree Lagna',   'NAKSHATRA_FRACTION', 1,
            N'Prosperity (Sree = wealth / Lakshmi). The reference point for Sudasa ("Sree Lagna Kendradi Rasi Dasa") (PVR sec 5.7).', 4,
            N'PVR sec 5.8: Sree Lagna moves at about twice the rate of the normal lagna; watch rasi-border cases.');
GO

-- --- Batch 2: tbl_Rule_SpecialLagnaTimeRate (Bhaava / Hora / Ghati) ---
-- Longitude = ( Sun's nirayana longitude at the day's opening sunrise )
--           + DegreesPerMinute * (birth clock time - sunrise clock time, in minutes),
--           normalized mod 360.
-- For a night birth the minute difference is negative (birth precedes the
-- opening sunrise) - correct, the lagna runs behind the Sun. Matches the
-- convention in HoraLagnaCalculator.cs.
IF OBJECT_ID('dbo.tbl_Rule_SpecialLagnaTimeRate', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_SpecialLagnaTimeRate (
        Id                   INT IDENTITY(1,1) NOT NULL
                                 CONSTRAINT PK_Rule_SpecialLagnaTimeRate PRIMARY KEY,
        RuleSetId            TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_SpecialLagnaTimeRate_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        SpecialLagnaId       TINYINT      NOT NULL
                                 CONSTRAINT FK_Rule_SpecialLagnaTimeRate_Lagna FOREIGN KEY REFERENCES dbo.tbl_Dim_SpecialLagnas (Id),
        AnchorPoint          VARCHAR(20)  NOT NULL,          -- SUN_AT_SUNRISE
        DegreesPerMinute     DECIMAL(9,6) NOT NULL,          -- Bhaava 0.25, Hora 0.5, Ghati 1.25
        NormalizationMethod  VARCHAR(12)  NOT NULL CONSTRAINT DF_Rule_SpecialLagnaTimeRate_Norm DEFAULT 'MOD_360',
        MethodCode           VARCHAR(30)  NULL,              -- 'TIME_RATE_FROM_SUNRISE'
        RuleParametersJson   NVARCHAR(MAX) NULL,
        CalculationNarrative NVARCHAR(MAX) NULL,
        SourceRefCode        VARCHAR(40)  NULL,
        IsActive             BIT          NOT NULL CONSTRAINT DF_Rule_SpecialLagnaTimeRate_IsActive DEFAULT 1,
        CONSTRAINT CK_Rule_SpecialLagnaTimeRate_Anchor CHECK (AnchorPoint IN ('SUN_AT_SUNRISE')),
        CONSTRAINT CK_Rule_SpecialLagnaTimeRate_Rate   CHECK (DegreesPerMinute > 0),
        CONSTRAINT CK_Rule_SpecialLagnaTimeRate_Json   CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_SpecialLagnaTimeRate_Src    CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_SpecialLagnaTimeRate UNIQUE (RuleSetId, SpecialLagnaId)
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SpecialLagnaTimeRate)
    INSERT dbo.tbl_Rule_SpecialLagnaTimeRate
        (RuleSetId, SpecialLagnaId, AnchorPoint, DegreesPerMinute, NormalizationMethod,
         MethodCode, CalculationNarrative, SourceRefCode, IsActive)
    SELECT 1, l.Id, v.AnchorPoint, CONVERT(DECIMAL(9,6), v.DegreesPerMinute), 'MOD_360',
           'TIME_RATE_FROM_SUNRISE', v.Narrative, 'SRC_PVR_INTEGRATED', 1
    FROM (VALUES
        ('BHAAVA_LAGNA', 'SUN_AT_SUNRISE', 0.250000,
            N'Bhaava Lagna (BL) = Sun''s nirayana longitude at the day''s opening sunrise + 0.25 deg per minute elapsed since that sunrise, mod 360. 0.25 deg/min = 15 deg/hour = one rasi per 2 hours, per PVR sec 5.2''s stated rate, the classical ishtakaala/5 rule, and JHora / PyJHora. ERRATUM NOTE: PVR sec 5.2''s method step (2) and worked Example 7 both take the minutes-since-sunrise value directly as degrees (i.e. 1.0 deg/min), which contradicts the same section''s rate by a factor of 4; that reading is treated as a book erratum (rammyps, 2026-09-04). BL carries UsedInBook = 0 - PVR defines it "only for the sake of completeness".'),
        ('HORA_LAGNA', 'SUN_AT_SUNRISE', 0.500000,
            N'Hora Lagna (HL) = Sun''s nirayana longitude at the day''s opening sunrise + 0.5 deg per minute elapsed since that sunrise, mod 360 (one rasi per hour). PVR sec 5.3. Matches the shipped HoraLagnaCalculator.cs and its verify-jaimini golden value (23 Pi 55'' 08'').'),
        ('GHATI_LAGNA', 'SUN_AT_SUNRISE', 1.250000,
            N'Ghati Lagna (GL) = Sun''s nirayana longitude at the day''s opening sunrise + 1.25 deg per minute elapsed since that sunrise, mod 360 (one rasi per ghati = 24 minutes; PVR''s step multiplies the minute difference by 5 and divides by 4). PVR sec 5.4.')
    ) v (LagnaCode, AnchorPoint, DegreesPerMinute, Narrative)
    JOIN dbo.tbl_Dim_SpecialLagnas l ON l.LagnaCode = v.LagnaCode;
GO

-- --- Batch 3: tbl_Rule_SpecialLagnaFraction (Sree Lagna) ---
-- Longitude = ( natal D1 lagna longitude )
--           + ( Moon's advancement through its nakshatra / 13 deg 20 min ) * 360,
--           normalized mod 360.
IF OBJECT_ID('dbo.tbl_Rule_SpecialLagnaFraction', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Rule_SpecialLagnaFraction (
        Id                    INT IDENTITY(1,1) NOT NULL
                                  CONSTRAINT PK_Rule_SpecialLagnaFraction PRIMARY KEY,
        RuleSetId             TINYINT      NOT NULL
                                  CONSTRAINT FK_Rule_SpecialLagnaFraction_RuleSet FOREIGN KEY REFERENCES dbo.tbl_Rule_Sets (Id),
        SpecialLagnaId        TINYINT      NOT NULL
                                  CONSTRAINT FK_Rule_SpecialLagnaFraction_Lagna FOREIGN KEY REFERENCES dbo.tbl_Dim_SpecialLagnas (Id),
        AnchorPoint           VARCHAR(20)  NOT NULL,          -- NATAL_LAGNA
        IncrementBodyPlanetId TINYINT      NOT NULL           -- the body whose nakshatra fraction is taken (Moon)
                                  CONSTRAINT FK_Rule_SpecialLagnaFraction_Body FOREIGN KEY REFERENCES dbo.tbl_Planets (Id),
        FractionBasis         VARCHAR(16)  NOT NULL,          -- NAKSHATRA (fraction of the 13 deg 20 min nakshatra)
        ScaleDegrees          DECIMAL(9,6) NOT NULL,          -- 360 - the fraction is applied to the whole zodiac
        NormalizationMethod   VARCHAR(12)  NOT NULL CONSTRAINT DF_Rule_SpecialLagnaFraction_Norm DEFAULT 'MOD_360',
        MethodCode            VARCHAR(30)  NULL,              -- 'LAGNA_PLUS_NAK_FRACTION'
        RuleParametersJson    NVARCHAR(MAX) NULL,
        CalculationNarrative  NVARCHAR(MAX) NULL,
        SourceRefCode         VARCHAR(40)  NULL,
        IsActive              BIT          NOT NULL CONSTRAINT DF_Rule_SpecialLagnaFraction_IsActive DEFAULT 1,
        CONSTRAINT CK_Rule_SpecialLagnaFraction_Anchor CHECK (AnchorPoint IN ('NATAL_LAGNA')),
        CONSTRAINT CK_Rule_SpecialLagnaFraction_Basis  CHECK (FractionBasis IN ('NAKSHATRA')),
        CONSTRAINT CK_Rule_SpecialLagnaFraction_Scale  CHECK (ScaleDegrees > 0),
        CONSTRAINT CK_Rule_SpecialLagnaFraction_Json   CHECK (RuleParametersJson IS NULL OR ISJSON(RuleParametersJson) = 1),
        CONSTRAINT CK_Rule_SpecialLagnaFraction_Src    CHECK (SourceRefCode IS NULL OR SourceRefCode LIKE 'SRC[_]%'),
        CONSTRAINT UQ_Rule_SpecialLagnaFraction UNIQUE (RuleSetId, SpecialLagnaId)
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SpecialLagnaFraction)
    INSERT dbo.tbl_Rule_SpecialLagnaFraction
        (RuleSetId, SpecialLagnaId, AnchorPoint, IncrementBodyPlanetId, FractionBasis, ScaleDegrees,
         NormalizationMethod, MethodCode, CalculationNarrative, SourceRefCode, IsActive)
    SELECT 1, l.Id, 'NATAL_LAGNA', p.Id, 'NAKSHATRA', CONVERT(DECIMAL(9,6), 360.000000), 'MOD_360',
           'LAGNA_PLUS_NAK_FRACTION',
           N'Sree Lagna (SL), PVR sec 5.7: (1) find the nakshatra occupied by the Moon; (2) find the fraction of that nakshatra the Moon has traversed; (3) take the same fraction of 360 deg; (4) add it to the natal lagna longitude, mod 360. Example 10: Moon 13 Li 06 in Swati (6 deg 40 min - 20 deg 00 min Li), fraction (6 deg 26 min)/(13 deg 20 min) = 0.4825, x 360 = 173 deg 42 min, + lagna 25 Vi 05 (175 deg 05 min) = 348 deg 47 min = 18 Pi 47.',
           'SRC_PVR_INTEGRATED', 1
    FROM dbo.tbl_Dim_SpecialLagnas l
    JOIN dbo.tbl_Planets p ON p.PlanetName = 'Moon'
    WHERE l.LagnaCode = 'SREE_LAGNA';
GO

-- --- Batch 4: tbl_Rule_Catalog registration ---
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_SpecialLagnaTimeRate')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_SpecialLagnaTimeRate', 'SPECIALLAGNA', 'TIME_RATE_FROM_SUNRISE',
            'Bhaava / Hora / Ghati Lagna: each is the Sun''s sunrise longitude plus a fixed DegreesPerMinute advance for every minute since the day''s opening sunrise (Bhaava 0.25, Hora 0.5, Ghati 1.25), mod 360. Reference data - only Hora Lagna is built in C#.',
            '28_add_special_lagna_rule_layer.sql');
IF NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_Catalog WHERE RuleTableName = 'tbl_Rule_SpecialLagnaFraction')
    INSERT dbo.tbl_Rule_Catalog (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
    VALUES ('tbl_Rule_SpecialLagnaFraction', 'SPECIALLAGNA', 'LAGNA_PLUS_NAK_FRACTION',
            'Sree Lagna: natal lagna longitude plus the Moon''s fraction through its nakshatra scaled to 360 deg, mod 360. Reference point for Sudasa. Reference data - engine not yet built.',
            '28_add_special_lagna_rule_layer.sql');
GO

-- --- Batch 5: ledger + summary ---
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '28_add_special_lagna_rule_layer.sql',
       'tbl_Dim_SpecialLagnas(4) + tbl_Rule_SpecialLagnaTimeRate(3: BL/HL/GL) + tbl_Rule_SpecialLagnaFraction(1: SL); 2 catalog rows. PVR ch 5. D1 reference data; only Hora Lagna built in C#.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '28_add_special_lagna_rule_layer.sql');
GO

DECLARE @master INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_SpecialLagnas);
DECLARE @rate   INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_SpecialLagnaTimeRate);
DECLARE @frac   INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_SpecialLagnaFraction);
-- every TIME_FROM_SUNRISE lagna has exactly one time-rate row; every
-- NAKSHATRA_FRACTION lagna has exactly one fraction row (RuleSetId 1).
DECLARE @ratemiss INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_SpecialLagnas l
    WHERE l.CalculationType = 'TIME_FROM_SUNRISE'
      AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SpecialLagnaTimeRate r WHERE r.SpecialLagnaId = l.Id AND r.RuleSetId = 1));
DECLARE @fracmiss INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_SpecialLagnas l
    WHERE l.CalculationType = 'NAKSHATRA_FRACTION'
      AND NOT EXISTS (SELECT 1 FROM dbo.tbl_Rule_SpecialLagnaFraction r WHERE r.SpecialLagnaId = l.Id AND r.RuleSetId = 1));
-- cross-family leakage: a time-rate row for a fraction lagna, or vice versa.
DECLARE @crossrate INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_SpecialLagnaTimeRate r
    JOIN dbo.tbl_Dim_SpecialLagnas l ON l.Id = r.SpecialLagnaId
    WHERE l.CalculationType <> 'TIME_FROM_SUNRISE');
DECLARE @crossfrac INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_SpecialLagnaFraction r
    JOIN dbo.tbl_Dim_SpecialLagnas l ON l.Id = r.SpecialLagnaId
    WHERE l.CalculationType <> 'NAKSHATRA_FRACTION');
-- pin the shipped Hora Lagna coefficient.
DECLARE @horabad INT = (SELECT COUNT(*) FROM dbo.tbl_Rule_SpecialLagnaTimeRate r
    JOIN dbo.tbl_Dim_SpecialLagnas l ON l.Id = r.SpecialLagnaId
    WHERE l.LagnaCode = 'HORA_LAGNA' AND r.DegreesPerMinute <> 0.5);
DECLARE @badsrc INT = (
    (SELECT COUNT(*) FROM dbo.tbl_Rule_SpecialLagnaTimeRate  WHERE SourceRefCode <> 'SRC_PVR_INTEGRATED')
  + (SELECT COUNT(*) FROM dbo.tbl_Rule_SpecialLagnaFraction  WHERE SourceRefCode <> 'SRC_PVR_INTEGRATED'));
PRINT '28 applied: ' + CAST(@master AS VARCHAR(10)) + ' master (expect 4), '
    + CAST(@rate AS VARCHAR(10)) + ' time-rate (expect 3), '
    + CAST(@frac AS VARCHAR(10)) + ' fraction (expect 1), '
    + CAST(@ratemiss AS VARCHAR(10)) + ' TIME_FROM_SUNRISE lagnas w/o a rate row (expect 0), '
    + CAST(@fracmiss AS VARCHAR(10)) + ' NAKSHATRA_FRACTION lagnas w/o a fraction row (expect 0), '
    + CAST(@crossrate AS VARCHAR(10)) + ' + ' + CAST(@crossfrac AS VARCHAR(10)) + ' cross-family rows (expect 0 + 0), '
    + CAST(@horabad AS VARCHAR(10)) + ' Hora rows not 0.5 deg/min (expect 0), '
    + CAST(@badsrc AS VARCHAR(10)) + ' seeded rows not SRC_PVR_INTEGRATED (expect 0).';
GO
