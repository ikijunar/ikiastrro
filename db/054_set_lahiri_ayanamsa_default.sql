USE [ikiastrro];
GO

-- Filtered unique index UX_Rule_Ayanamsa_Default requires QUOTED_IDENTIFIER ON for writes.
SET QUOTED_IDENTIFIER ON;
GO

-- ---------------------------------------------------------------------------
-- 36_create_rule_ayanamsa.sql seeded AYANAMSA_JAGANNATHA (Swiss sidereal mode
-- 26 = SE_SIDM_SS_CITRA) as the active default. That mode gives ~22.745 deg for
-- 1981 against the JHora reference 23.595 deg (Lahiri / Chitrapaksha), a ~0.85
-- deg error that fails verify-vargas and verify-jaimini once chart generation
-- reads this row (it did from commit c238aa8 on).
--
-- Mode 27 (True Chitrapaksha) matches the reference but needs Swiss data file
-- sefstars.txt, which this file-less Moshier build does not ship. Mode 1
-- (SE_SIDM_LAHIRI) is the polynomial model this project has always calculated
-- with and every verify-* reference chart is built in. Make it the default.
-- ---------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM dbo.tbl_Rule_Ayanamsa
           WHERE RuleSetId = 1 AND IsDefault = 1 AND Code <> 'AYANAMSA_LAHIRI')
BEGIN
    -- One statement: SQL Server validates the filtered unique index once at
    -- statement end, so the old default and the new default never coexist.
    UPDATE dbo.tbl_Rule_Ayanamsa
    SET IsDefault = CASE WHEN Code = 'AYANAMSA_LAHIRI' THEN 1 ELSE 0 END
    WHERE RuleSetId = 1;
END
GO

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '054_set_lahiri_ayanamsa_default.sql',
       'Repoint tbl_Rule_Ayanamsa active default from Jagannatha (mode 26) to Lahiri (mode 1) to match the JHora reference and verify-* charts.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations
                  WHERE ScriptName = '054_set_lahiri_ayanamsa_default.sql');
GO
