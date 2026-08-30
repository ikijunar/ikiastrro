-- vedic_horo_gen: database creation
-- Run once against your SQL Server instance (Windows Auth, localhost/RAMMYPS).
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'vedic_horo_gen')
BEGIN
    CREATE DATABASE vedic_horo_gen;
END
GO
