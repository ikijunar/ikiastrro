-- =====================================================================
-- 50 — Complete tbl_Rule_Catalog coverage for rule tables introduced by
-- migrations 36, 39, and 46.
-- =====================================================================
USE [ikiastrro];
GO

IF OBJECT_ID('dbo.tbl_Rule_Catalog', 'U') IS NULL
    THROW 50050, 'Migration 50 requires dbo.tbl_Rule_Catalog.', 1;

INSERT dbo.tbl_Rule_Catalog
    (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
SELECT v.RuleTableName, v.EngineCode, v.MethodCodes, v.Purpose, v.IntroducedIn
FROM (VALUES
    ('tbl_Rule_Ayanamsa', 'ASTRONOMY', 'SIDEREAL_MODE,USER_OFFSET',
     'Versioned ayanamsa selection, Swiss Ephemeris sidereal mode, optional correction, and default policy.',
     '36_create_rule_ayanamsa.sql'),
    ('tbl_Rule_BhavaBalaComponent', 'STRENGTH', 'HOUSE_LORD_SHADBALA,HOUSE_DIRECTION,HOUSE_ASPECT',
     'Versioned Bhava Bala component definitions, maxima, methods, and formula provenance.',
     '39_add_shadbala_strength_facts.sql'),
    ('tbl_Rule_DashaApplicability', 'DASHA', 'CONDITION_LOOKUP',
     'Source-attributed applicability conditions for conditional dasha systems.',
     '46_create_ayanamsa_dasha_benchmarks.sql')
) v (RuleTableName, EngineCode, MethodCodes, Purpose, IntroducedIn)
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.tbl_Rule_Catalog c
    WHERE c.RuleTableName = v.RuleTableName
);

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '50_complete_rule_catalog.sql',
       'Register ayanamsa, Bhava Bala component, and dasha-applicability rule tables.'
WHERE NOT EXISTS
    (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '50_complete_rule_catalog.sql');
GO

PRINT '50 applied: tbl_Rule_Catalog coverage completed.';
GO
