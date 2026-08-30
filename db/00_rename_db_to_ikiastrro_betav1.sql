-- =====================================================================
-- One-time: rename the existing 'vedic_horo_gen' database to 'ikiastrro_betav1'
-- (preserves all data - the 6 saved people, every chart, all reference tables).
--
-- Run this from a NEW query window connected to [master], with the ikiastrro
-- app and any other tools NOT connected to the database. SET SINGLE_USER with
-- ROLLBACK IMMEDIATE force-closes other sessions (e.g. an idle SSMS tab) so the
-- rename can take the required exclusive lock; SET MULTI_USER re-opens it.
--
-- After this, the app's connection string already points at [ikiastrro_betav1]
-- (SqlConnectionFactory.cs) - nothing else to change.
--
-- For a brand-new machine with no existing database, skip this file and just run
-- db/ikiastrro_betav1.sql instead (it builds the whole schema + reference data).
-- =====================================================================
USE [master];
GO

IF DB_ID(N'ikiastrro_betav1') IS NOT NULL
BEGIN
    PRINT 'ikiastrro_betav1 already exists - nothing to do.';
END
ELSE IF DB_ID(N'vedic_horo_gen') IS NULL
BEGIN
    PRINT 'Neither vedic_horo_gen nor ikiastrro_betav1 exists - run db/ikiastrro_betav1.sql for a fresh build.';
END
ELSE
BEGIN
    ALTER DATABASE [vedic_horo_gen] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    ALTER DATABASE [vedic_horo_gen] MODIFY NAME = [ikiastrro_betav1];
    ALTER DATABASE [ikiastrro_betav1] SET MULTI_USER;
    PRINT 'Renamed vedic_horo_gen -> ikiastrro_betav1.';
END
GO

SELECT name, state_desc, user_access_desc, create_date
FROM sys.databases
WHERE name IN (N'vedic_horo_gen', N'ikiastrro_betav1');
GO
