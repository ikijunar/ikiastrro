-- 35 — JHora ayanamsa preference catalogue
-- Adds the 22 named options found in preferences_tree.xlsx to the terminology registry.
-- NumericKey is the Swiss Ephemeris sidereal-mode ID where a direct implementation exists.
USE [ikiastrro];
GO
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '35_seed_ayanamsa_catalog.sql')
BEGIN
    MERGE dbo.tbl_Astro_Terminology AS tgt
    USING (VALUES
      ('AYANAMSA_TRUE_LAHIRI', 'True Lahiri/Chitrapaksha', 27, 1),
      ('AYANAMSA_LAHIRI', 'Traditional Lahiri', 1, 2),
      ('AYANAMSA_PUSHYA_PAKSHA', 'Pushya-paksha ayanamsa', 29, 3),
      ('AYANAMSA_RAMAN', 'Raman', 3, 4),
      ('AYANAMSA_KP', 'Krishnamoorthy (KP)', 5, 5),
      ('AYANAMSA_FIXED_STAR_CUSTOM', 'Fixed star based CUSTOM ayanamsa', NULL, 6),
      ('AYANAMSA_JAGANNATHA', 'Jagannatha', 26, 7),
      ('AYANAMSA_ROHINI_PAKSHA', 'Rohini-paksha ayanamsa', NULL, 8),
      ('AYANAMSA_SRI_SURYA_SIDDHANTA', 'Sri Surya Siddhanta', 21, 9),
      ('AYANAMSA_DEVA_DATTA', 'Deva-datta', NULL, 10),
      ('AYANAMSA_USHA_SHASHI', 'Usha-Shashi', 4, 11),
      ('AYANAMSA_YUKTESHWAR', 'Yukteshwar', 7, 12),
      ('AYANAMSA_JN_BHASIN', 'JN Bhasin', 8, 13),
      ('AYANAMSA_CHANDRA_HARI', 'Chandra Hari', NULL, 14),
      ('AYANAMSA_FAGAN', 'Fagan', 0, 15),
      ('AYANAMSA_DELUCE', 'Deluce', 2, 16),
      ('AYANAMSA_DJWHAL_KHUL', 'Djwhal Khul', 6, 17),
      ('AYANAMSA_ALDEBARAN_15_TAU', 'Aldebaran at 15Ta0', 14, 18),
      ('AYANAMSA_GALACTIC_CENTER', 'Galaxy center at 0Sg0', 17, 19),
      ('AYANAMSA_HIPPARCHOS', 'Hipparchos', 15, 20),
      ('AYANAMSA_SASSANIAN', 'Sassanian', 16, 21),
      ('AYANAMSA_TROPICAL', 'Tropical (sayana)', NULL, 22)
    ) AS src(Code, DisplayName, NumericKey, DisplayOrder)
    ON tgt.Code = src.Code
    WHEN MATCHED THEN UPDATE SET Category = 'Ayanamsa', EngineCode = 'ASTRO_CALC',
        NumericKey = src.NumericKey, DisplayOrder = src.DisplayOrder, IsActive = 1,
        FormulaSummary = src.DisplayName
    WHEN NOT MATCHED THEN INSERT (Category, Code, EngineCode, NumericKey, FormulaSummary, DisplayOrder, IsActive)
        VALUES ('Ayanamsa', src.Code, 'ASTRO_CALC', src.NumericKey, src.DisplayName, src.DisplayOrder, 1);

    MERGE dbo.tbl_Astro_TerminologyText AS tgt
    USING (
        SELECT t.TerminologyId, v.Name
        FROM dbo.tbl_Astro_Terminology t
        JOIN (VALUES
          ('AYANAMSA_TRUE_LAHIRI','True Lahiri/Chitrapaksha'),('AYANAMSA_LAHIRI','Traditional Lahiri'),
          ('AYANAMSA_PUSHYA_PAKSHA','Pushya-paksha ayanamsa'),('AYANAMSA_RAMAN','Raman'),
          ('AYANAMSA_KP','Krishnamoorthy (KP)'),('AYANAMSA_FIXED_STAR_CUSTOM','Fixed star based CUSTOM ayanamsa'),
          ('AYANAMSA_JAGANNATHA','Jagannatha'),('AYANAMSA_ROHINI_PAKSHA','Rohini-paksha ayanamsa'),
          ('AYANAMSA_SRI_SURYA_SIDDHANTA','Sri Surya Siddhanta'),('AYANAMSA_DEVA_DATTA','Deva-datta'),
          ('AYANAMSA_USHA_SHASHI','Usha-Shashi'),('AYANAMSA_YUKTESHWAR','Yukteshwar'),
          ('AYANAMSA_JN_BHASIN','JN Bhasin'),('AYANAMSA_CHANDRA_HARI','Chandra Hari'),
          ('AYANAMSA_FAGAN','Fagan'),('AYANAMSA_DELUCE','Deluce'),('AYANAMSA_DJWHAL_KHUL','Djwhal Khul'),
          ('AYANAMSA_ALDEBARAN_15_TAU','Aldebaran at 15Ta0'),('AYANAMSA_GALACTIC_CENTER','Galaxy center at 0Sg0'),
          ('AYANAMSA_HIPPARCHOS','Hipparchos'),('AYANAMSA_SASSANIAN','Sassanian'),('AYANAMSA_TROPICAL','Tropical (sayana)')
        ) v(Code, Name) ON v.Code = t.Code
    ) AS src(TerminologyId, Name)
    ON tgt.TerminologyId = src.TerminologyId AND tgt.LanguageCode = 'en' AND tgt.Script = 'Latn'
    WHEN MATCHED THEN UPDATE SET Name = src.Name
    WHEN NOT MATCHED THEN INSERT (TerminologyId, LanguageCode, Script, Name)
        VALUES (src.TerminologyId, 'en', 'Latn', src.Name);

    INSERT dbo.SchemaMigrations (ScriptName, Note)
    VALUES ('35_seed_ayanamsa_catalog.sql', 'JHora 22-option ayanamsa catalogue and Swiss mode mapping');
    PRINT '35 applied: ayanamsa catalogue seeded.';
END
ELSE PRINT '35 already applied.';
GO
