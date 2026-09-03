-- =====================================================================
-- 01 — Schema-migration ledger. First numbered migration; the historical
-- db/00_*.sql one-offs predate it and are not recorded here.
-- Idempotent. Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/01_create_schema_migrations.sql
-- =====================================================================
USE [ikiastrro];
GO
IF OBJECT_ID('dbo.SchemaMigrations', 'U') IS NULL
CREATE TABLE dbo.SchemaMigrations (
    ScriptName    VARCHAR(120)  NOT NULL CONSTRAINT PK_SchemaMigrations PRIMARY KEY,
    AppliedAtUtc  DATETIME2(0)  NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedAtUtc DEFAULT sysutcdatetime(),
    ScriptHash    CHAR(64)      NULL,
    Note          VARCHAR(200)  NULL
);
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '01_create_schema_migrations.sql', 'ledger bootstrap'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '01_create_schema_migrations.sql');
GO
PRINT '01 applied: dbo.SchemaMigrations ready.';
GO
