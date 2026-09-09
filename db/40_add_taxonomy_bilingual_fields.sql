-- =====================================================================
-- 40 - Bilingual taxonomy fields.
--
-- Taxonomy dimensions keep a stable English label and a transliterated
-- Sanskrit label beside their codes. The terminology catalogue remains the
-- localized presentation layer; these columns make exports and rule joins
-- self-describing without requiring a text-table join.
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @tables TABLE (TableName SYSNAME, EnglishColumn SYSNAME, SanskritColumn SYSNAME);
INSERT @tables VALUES
 ('tbl_Dim_LifeArea','EnglishName','SanskritName'),
 ('tbl_Dim_House','EnglishName','SanskritName'),
 ('tbl_Dim_HouseCategory','EnglishName','SanskritName'),
 ('tbl_Dim_HouseReference','EnglishName','SanskritName'),
 ('tbl_Dim_DivisionalSubject','EnglishName','SanskritName'),
 ('tbl_Dim_InterpretationDimension','EnglishName','SanskritName');

DECLARE @table SYSNAME, @sql NVARCHAR(MAX);
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT TableName FROM @tables;
OPEN c;
FETCH NEXT FROM c INTO @table;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID('dbo.' + @table, 'U') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.' + @table, 'EnglishName') IS NULL
        BEGIN
            SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@table) + N' ADD EnglishName NVARCHAR(160) NULL;';
            EXEC sys.sp_executesql @sql;
        END
        IF COL_LENGTH('dbo.' + @table, 'SanskritName') IS NULL
        BEGIN
            SET @sql = N'ALTER TABLE dbo.' + QUOTENAME(@table) + N' ADD SanskritName NVARCHAR(160) NULL;';
            EXEC sys.sp_executesql @sql;
        END
    END;
    FETCH NEXT FROM c INTO @table;
END
CLOSE c; DEALLOCATE c;
GO

-- Preserve existing English labels while providing explicit Sanskrit labels.
UPDATE dbo.tbl_Dim_LifeArea SET EnglishName = AreaName,
    SanskritName = CASE AreaCode
      WHEN 'PHYSICAL_EXISTENCE' THEN N'Sthula Deha' WHEN 'WEALTH' THEN N'Dhana'
      WHEN 'SIBLINGS' THEN N'Sahaja' WHEN 'PROPERTY_FORTUNE' THEN N'Griha Bhagya'
      WHEN 'FAME_POWER' THEN N'Yasha Adhikara' WHEN 'HEALTH_TROUBLES' THEN N'Roga'
      WHEN 'CHILDREN' THEN N'Santana' WHEN 'SUDDEN_TROUBLES' THEN N'Akasmika Vighna'
      WHEN 'MARRIAGE_SPOUSE' THEN N'Kalatra' WHEN 'CAREER' THEN N'Karma Ajiva'
      WHEN 'DEATH_DESTRUCTION' THEN N'Mrityu Nasha' WHEN 'PARENTS' THEN N'Matri Pitri'
      WHEN 'VEHICLES_COMFORTS' THEN N'Vahana Sukha' WHEN 'RELIGION_SPIRITUALITY' THEN N'Dharma Adhyatma'
      WHEN 'EDUCATION' THEN N'Vidya' WHEN 'INNATE_NATURE' THEN N'Svabhava'
      WHEN 'EVILS_PUNISHMENT' THEN N'Dushkrita Danda' WHEN 'AUSPICIOUS_EVENTS' THEN N'Shubha Ashubha'
      WHEN 'ALL_MATTERS' THEN N'Sarva Vishaya' WHEN 'PAST_LIFE_KARMA' THEN N'Prarabdha Karma'
    END;
UPDATE dbo.tbl_Dim_House SET EnglishName = ShortName, SanskritName = BhavaNameSa;
UPDATE dbo.tbl_Dim_HouseCategory SET EnglishName = DisplayName,
    SanskritName = CASE CategoryCode
      WHEN 'KENDRA' THEN N'Kendra' WHEN 'TRIKONA' THEN N'Trikona' WHEN 'PANAPHARA' THEN N'Panaphara'
      WHEN 'APOKLIMA' THEN N'Apoklima' WHEN 'UPACHAYA' THEN N'Upachaya' WHEN 'DUSTHANA' THEN N'Dusthana'
      WHEN 'CHATURASRA' THEN N'Chaturasra' WHEN 'MARAKA' THEN N'Maraka' END;
UPDATE dbo.tbl_Dim_HouseReference SET EnglishName = ReferenceName,
    SanskritName = CASE ReferenceCode
      WHEN 'LAGNA' THEN N'Lagna' WHEN 'CHANDRA_LAGNA' THEN N'Chandra Lagna'
      WHEN 'RAVI_LAGNA' THEN N'Ravi Lagna' WHEN 'ARUDHA_LAGNA' THEN N'Arudha Lagna'
      WHEN 'PAAKA_LAGNA' THEN N'Paaka Lagna' WHEN 'KARAKAMSA_LAGNA' THEN N'Karakamsa Lagna'
      WHEN 'GHATI_LAGNA' THEN N'Ghati Lagna' WHEN 'HORA_LAGNA' THEN N'Hora Lagna'
      WHEN 'BHAAVA_LAGNA' THEN N'Bhaava Lagna' WHEN 'SREE_LAGNA' THEN N'Sree Lagna'
      WHEN 'GRAHA_LAGNA_SUN' THEN N'Surya Graha Lagna' WHEN 'GRAHA_LAGNA_MOON' THEN N'Chandra Graha Lagna'
      WHEN 'GRAHA_LAGNA_MARS' THEN N'Mangala Graha Lagna' WHEN 'GRAHA_LAGNA_MERCURY' THEN N'Budha Graha Lagna'
      WHEN 'GRAHA_LAGNA_JUPITER' THEN N'Guru Graha Lagna' WHEN 'GRAHA_LAGNA_VENUS' THEN N'Shukra Graha Lagna'
      WHEN 'GRAHA_LAGNA_SATURN' THEN N'Shani Graha Lagna' END;
UPDATE dbo.tbl_Dim_DivisionalSubject SET EnglishName = SubjectName,
    SanskritName = CASE SubjectCode
      WHEN 'OVERALL_STRENGTH_DHARMA' THEN N'Sarva Bala Dharma' WHEN 'WEALTH' THEN N'Dhana'
      WHEN 'SIBLINGS_COURAGE' THEN N'Sahaja Shaurya' WHEN 'PROPERTY_RESIDENCE' THEN N'Griha Vahana'
      WHEN 'CHILDREN_PROGENY' THEN N'Santana' WHEN 'MOTHER_PARENTS' THEN N'Matri Pitri'
      WHEN 'MARRIAGE_RELATIONSHIPS' THEN N'Kalatra Sambandha' WHEN 'CAREER_STATUS' THEN N'Karma Pratishtha'
      WHEN 'VEHICLES_COMFORTS' THEN N'Vahana Sukha' WHEN 'EDUCATION_LEARNING' THEN N'Vidya Adhyayana'
      WHEN 'KARMIC_ROOTS' THEN N'Purva Janma Karma' END;
UPDATE dbo.tbl_Dim_InterpretationDimension SET EnglishName = DimensionName,
    SanskritName = CASE DimensionCode
      WHEN 'PLANETARY_DIGNITY' THEN N'Graha Bala' WHEN 'DISPOSITOR_RELATION' THEN N'Bhaavesha Sambandha'
      WHEN 'HOUSE_LORD_COMBINATION' THEN N'Bhava Adhipati Yoga' WHEN 'DIVISIONAL_CONFIRMATION' THEN N'Varga Pushti'
    END;
GO

-- These labels are part of the taxonomy contract: after the backfill, they
-- are required for every current row. The guards keep the migration rerunnable.
DECLARE @t SYSNAME, @sql2 NVARCHAR(MAX);
DECLARE c2 CURSOR LOCAL FAST_FORWARD FOR
    SELECT TableName FROM (VALUES
      ('tbl_Dim_LifeArea'),('tbl_Dim_House'),('tbl_Dim_HouseCategory'),
      ('tbl_Dim_HouseReference'),('tbl_Dim_DivisionalSubject'),('tbl_Dim_InterpretationDimension')
    ) v(TableName);
OPEN c2; FETCH NEXT FROM c2 INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql2 = N'IF NOT EXISTS (SELECT 1 FROM dbo.' + QUOTENAME(@t) + N' WHERE EnglishName IS NULL OR SanskritName IS NULL)
        ALTER TABLE dbo.' + QUOTENAME(@t) + N' ALTER COLUMN EnglishName NVARCHAR(160) NOT NULL;
        ALTER TABLE dbo.' + QUOTENAME(@t) + N' ALTER COLUMN SanskritName NVARCHAR(160) NOT NULL;';
    EXEC sys.sp_executesql @sql2;
    FETCH NEXT FROM c2 INTO @t;
END
CLOSE c2; DEALLOCATE c2;
GO

INSERT dbo.SchemaMigrations (ScriptName, AppliedAtUtc, Note)
SELECT '40_add_taxonomy_bilingual_fields.sql', SYSUTCDATETIME(),
       'Adds EnglishName and SanskritName to life-area, house, house-category, house-reference, divisional-subject and interpretation-dimension taxonomies.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '40_add_taxonomy_bilingual_fields.sql');
GO

SELECT t.name AS TableName, c.name AS ColumnName
FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id
WHERE t.name IN ('tbl_Dim_LifeArea','tbl_Dim_House','tbl_Dim_HouseCategory','tbl_Dim_HouseReference','tbl_Dim_DivisionalSubject','tbl_Dim_InterpretationDimension')
  AND c.name IN ('EnglishName','SanskritName')
ORDER BY t.name, c.name;
GO
