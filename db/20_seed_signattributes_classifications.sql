-- =====================================================================
-- 20 — tbl_SignAttributes: populate the classical classification columns
-- from "Vedic Astrology: An Integrated Approach" (P.V.R. Narasimha Rao),
-- sections 2.2.10-2.2.14:
--   Day_Night    2.2.10  divaa (Day) / nishaa (Night) rasis
--   RisingType   2.2.11  Sirshodaya (head) / Prishthodaya (feet) / Ubhayodaya (Pisces, both)
--   Varna_Class  2.2.12  Brahmanas / Kshatriyas / Vaisyas / Sudras
--   Guna         2.2.13  Sattwa / Rajas / Tamas
--   BodyType     2.2.14  Pitta / Vaata / Kapha / Mixed (Ge, Li, Aq = "mixed nature")
-- RisingType is the pre-existing schema column (NULL for every row until now);
-- the other four arrived in migration 19.
-- Foot / OddEven / OddEvenSanskrit are left NULL — not covered by this source.
-- Authoritative UPDATE (re-run resets to the same values). Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/20_seed_signattributes_classifications.sql
-- =====================================================================
USE [ikiastrro];
GO
;WITH c (Id, Day_Night, RisingType, Varna_Class, Guna, BodyType) AS (
    SELECT * FROM (VALUES
        ( 1, 'Night', 'Prishthodaya', 'Kshatriyas', 'Rajas',  'Pitta'),
        ( 2, 'Night', 'Prishthodaya', 'Vaisyas',    'Rajas',  'Vaata'),
        ( 3, 'Night', 'Sirshodaya',   'Sudras',     'Tamas',  'Mixed'),
        ( 4, 'Night', 'Prishthodaya', 'Brahmanas',  'Sattwa', 'Kapha'),
        ( 5, 'Day',   'Sirshodaya',   'Kshatriyas', 'Sattwa', 'Pitta'),
        ( 6, 'Day',   'Sirshodaya',   'Vaisyas',    'Tamas',  'Vaata'),
        ( 7, 'Day',   'Sirshodaya',   'Sudras',     'Rajas',  'Mixed'),
        ( 8, 'Day',   'Sirshodaya',   'Brahmanas',  'Rajas',  'Kapha'),
        ( 9, 'Night', 'Prishthodaya', 'Kshatriyas', 'Sattwa', 'Pitta'),
        (10, 'Night', 'Prishthodaya', 'Vaisyas',    'Tamas',  'Vaata'),
        (11, 'Day',   'Sirshodaya',   'Sudras',     'Tamas',  'Mixed'),
        (12, 'Day',   'Ubhayodaya',   'Brahmanas',  'Sattwa', 'Kapha')
    ) v (Id, Day_Night, RisingType, Varna_Class, Guna, BodyType)
)
UPDATE sa
   SET sa.Day_Night   = c.Day_Night,
       sa.RisingType   = c.RisingType,
       sa.Varna_Class  = c.Varna_Class,
       sa.Guna         = c.Guna,
       sa.BodyType     = c.BodyType
  FROM dbo.tbl_SignAttributes sa
  JOIN c ON c.Id = sa.Id;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '20_seed_signattributes_classifications.sql', 'tbl_SignAttributes Day_Night/RisingType/Varna_Class/Guna/BodyType seeded for all 12 signs (Narasimha Rao 2.2.10-2.2.14)'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '20_seed_signattributes_classifications.sql');
GO
PRINT '20 applied: tbl_SignAttributes classification values seeded.';
GO
