-- =====================================================================
-- 19 — tbl_SignAttributes: classical rasi classification columns.
-- Adds 8 nullable columns:
--   Foot            oddfooted / evenfooted
--   OddEven         Odd / Even
--   OddEvenSanskrit Vishama / Sama
--   BodyType        Pitta / Vaata / Kapha        (Ayurvedic dosha)
--   Guna            Sattwa / Rajas / Tamas
--   Day_Night       Day / Night
--   Varna_Class     Brahmanas / Kshatriyas / Vaisyas / Sudras
--   SignIndication  free text (nvarchar 1000) — seeded for all 12 signs below
-- Only SignIndication is populated here; the other 7 are structure-only for now.
-- Column adds are guarded by COL_LENGTH; the SignIndication UPDATEs are
-- authoritative (re-run resets to the same values). Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/19_add_signattributes_classifications.sql
-- =====================================================================
USE [ikiastrro];
GO
IF COL_LENGTH('dbo.tbl_SignAttributes', 'Foot')            IS NULL ALTER TABLE dbo.tbl_SignAttributes ADD [Foot]            varchar(11)    NULL;
GO
IF COL_LENGTH('dbo.tbl_SignAttributes', 'OddEven')         IS NULL ALTER TABLE dbo.tbl_SignAttributes ADD [OddEven]         varchar(4)     NULL;
GO
IF COL_LENGTH('dbo.tbl_SignAttributes', 'OddEvenSanskrit') IS NULL ALTER TABLE dbo.tbl_SignAttributes ADD [OddEvenSanskrit] varchar(10)    NULL;
GO
IF COL_LENGTH('dbo.tbl_SignAttributes', 'BodyType')        IS NULL ALTER TABLE dbo.tbl_SignAttributes ADD [BodyType]        varchar(10)    NULL;
GO
IF COL_LENGTH('dbo.tbl_SignAttributes', 'Guna')            IS NULL ALTER TABLE dbo.tbl_SignAttributes ADD [Guna]            varchar(10)    NULL;
GO
IF COL_LENGTH('dbo.tbl_SignAttributes', 'Day_Night')       IS NULL ALTER TABLE dbo.tbl_SignAttributes ADD [Day_Night]       varchar(5)     NULL;
GO
IF COL_LENGTH('dbo.tbl_SignAttributes', 'Varna_Class')     IS NULL ALTER TABLE dbo.tbl_SignAttributes ADD [Varna_Class]     varchar(11)    NULL;
GO
IF COL_LENGTH('dbo.tbl_SignAttributes', 'SignIndication')  IS NULL ALTER TABLE dbo.tbl_SignAttributes ADD [SignIndication]  nvarchar(1000) NULL;
GO
-- --- SignIndication seed (authoritative; safe to re-run) ---
UPDATE dbo.tbl_SignAttributes SET SignIndication = N'Dynamic, enterprising, valiant, ruddy, head, forests, large forehead, hasty, impulsive, restless, thick eyebrows, leadership, overbearing, dry, lean, tall.' WHERE Id = 1;
UPDATE dbo.tbl_SignAttributes SET SignIndication = N'Beautiful, face, stable, sluggish, loyal, meadows, plains, luxury halls, dining halls, eating places, fine teeth, large eyes, luxurious, faithful, thick hair, stout.' WHERE Id = 2;
UPDATE dbo.tbl_SignAttributes SET SignIndication = N'Chest, garden, communication, journalism, schools, colleges, study rooms, cables, telephone, newspapers, tall, well-built, prominent cheeks, thick hair, broad chest, curious, learned, jovial.' WHERE Id = 3;
UPDATE dbo.tbl_SignAttributes SET SignIndication = N'Heart, breast, watery fields, rivers, canals, kitchen, food, attractive, small build, emotional, deeply attached, mother-like, sensitive.' WHERE Id = 4;
UPDATE dbo.tbl_SignAttributes SET SignIndication = N'Stomach, digestion, navel, mountains, forests, caves, deserts, palaces, parks, forts, boilers, steel factories, thin, dry, hot, royal, self-pride, insolent, domineering.' WHERE Id = 5;
UPDATE dbo.tbl_SignAttributes SET SignIndication = N'Hip, appendix, lush gardens, fields, orchards, libraries, bookstores, farms, intelligent, sharp, orator, nervous, physically weak, discretion, tactfulness.' WHERE Id = 6;
UPDATE dbo.tbl_SignAttributes SET SignIndication = N'Groins, businessmen, markets, trade centers, banks, hotels, amusement parks, entertainment, toilets, cosmetics, balanced, wise, good talker.' WHERE Id = 7;
UPDATE dbo.tbl_SignAttributes SET SignIndication = N'Private parts, holes, deep caves, mines, garages, small build, dusky complexion, bright eyes, secretive, scheming, occult, best friend or a worst enemy, peevish, sensitive.' WHERE Id = 8;
UPDATE dbo.tbl_SignAttributes SET SignIndication = N'Thighs, royal, attorneys, government offices, aircraft, falling, sparse hair, muscular, deep eyes, upright, honest, genial, gambler.' WHERE Id = 9;
UPDATE dbo.tbl_SignAttributes SET SignIndication = N'Knees, marsh lands, watery places, alligators, beasts, bushes, slender build, long neck, prominent teeth, witty, perfectionist, patient, organizer, cautious, secretive, pragmatic.' WHERE Id = 10;
UPDATE dbo.tbl_SignAttributes SET SignIndication = N'Ankles, charity, philosophy, tall, bony, small eyes, mountain spring, places with water, ill-formed teeth, coarse hair, hard-working, stoic, honest.' WHERE Id = 11;
UPDATE dbo.tbl_SignAttributes SET SignIndication = N'Feet, oceans, seas, prisons, hospitals, hermitages, short, plump, large eyes, large eyebrows, lazy, emotional, timid, honest, irresolute, talkative, intuitive.' WHERE Id = 12;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '19_add_signattributes_classifications.sql', 'tbl_SignAttributes += Foot/OddEven/OddEvenSanskrit/BodyType/Guna/Day_Night/Varna_Class/SignIndication; SignIndication seeded for all 12 signs'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '19_add_signattributes_classifications.sql');
GO
PRINT '19 applied: tbl_SignAttributes classification columns added; SignIndication seeded.';
GO
