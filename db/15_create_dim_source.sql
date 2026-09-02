-- =====================================================================
-- 15 — tbl_Dim_Source: the SRC_* citation registry (STANDARDS §M.4). Mirror of
-- docs/research/reference-sources.md. Every SourceRefCode column added in later
-- plans (rule tables, terminology) FKs here by Code. Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -b -i db/15_create_dim_source.sql
-- =====================================================================
USE [ikiastrro];
GO
IF OBJECT_ID('dbo.tbl_Dim_Source', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.tbl_Dim_Source (
        Id        INT IDENTITY(1,1) CONSTRAINT PK_Dim_Source PRIMARY KEY,
        Code      VARCHAR(40)   NOT NULL CONSTRAINT UQ_Dim_Source_Code UNIQUE,
        Title     NVARCHAR(200) NOT NULL,
        Author    NVARCHAR(120) NULL,
        Edition   NVARCHAR(80)  NULL,
        Tradition VARCHAR(40)   NULL,
        Notes     NVARCHAR(400) NULL,
        IsActive  BIT           NOT NULL CONSTRAINT DF_Dim_Source_IsActive DEFAULT 1,
        CONSTRAINT CK_Dim_Source_Code CHECK (Code LIKE 'SRC[_]%')
    );
END
GO
-- Seed / re-seed (idempotent MERGE on Code)
;WITH src (Code, Title, Author, Edition, Tradition, Notes) AS (
    SELECT * FROM (VALUES
        ('SRC_BPHS',            N'Brihat Parashara Hora Shastra', N'Parāśara (attrib.)', NULL, 'Parasari', N'Umbrella; prefer a chapter-scoped code where known'),
        ('SRC_BPHS_26',         N'BPHS ch. 26 — Graha Drishti',   NULL, NULL, 'Parasari', N'7th full; Mars 4/8, Jupiter 5/9, Saturn 3/10'),
        ('SRC_BPHS_27',         N'BPHS ch. 27 — Shadbala',        NULL, NULL, 'Parasari', N'Strength engine (Plan 3); lookup tables need a cited edition'),
        ('SRC_BPHS_COMBUSTION', N'BPHS — Asta (combustion) orbs', NULL, NULL, 'Parasari', N'Moon 12, Mars 17/8R, Mercury 14/12R, Jupiter 11, Venus 10/8R, Saturn 15'),
        ('SRC_BPHS_AVASTHA',    N'BPHS — Baladi & Jagradadi avasthas', NULL, NULL, 'Parasari', N'Bala .25 / Kumara .50 / Yuva 1 / Vriddha .125 / Mrita 0'),
        ('SRC_PHALADEEPIKA',    N'Phaladeepika',                  N'Mantreshvara', NULL, 'classical', N'Combustion orb cross-check'),
        ('SRC_RAMAN_HTJH',      N'How to Judge a Horoscope (I–II)', N'B. V. Raman', NULL, 'Raman', N'House/Lagna significations, functional nature; OCR extract in BookExtracts'),
        ('SRC_RAMAN_HINDU_PREDICTIVE', N'Hindu Predictive Astrology', N'B. V. Raman', NULL, 'Raman', N'General'),
        ('SRC_PYJHORA',         N'PyJHora (source)',              N'pyjhora', N'_research/PyJHora', 'mixed', N'Varga formulae, special-lagna/upagraha algorithms; AGPL, vendored for reference'),
        ('SRC_JHORA',           N'Jagannatha Hora (desktop)',     N'P. V. R. Narasimha Rao', N'v8.x', 'mixed', N'Golden-record verification'),
        ('SRC_JHORA_EXPORT_RAMAKRISHNAN', N'JHora natal export — 1_Ramakrishnan', NULL, N'22 Apr 1981 05:30 Chennai', NULL, N'verify-vargas / verify-jaimini golden values; docs/artifacts/reference-charts/Rammy_Jagannatha.txt'),
        ('SRC_RATH_VARGA',      N'Vedic Astrology / varga methods', N'Sanjay Rath', NULL, 'Jaimini/SJC', N'D11 (Rudramsa), argala'),
        ('SRC_VEDASTRO',        N'VedAstro.Library',              N'(open source)', N'pre-2026-08-24', 'mixed', N'Historical — replaced by SwissEphNet; enum spellings inherited'),
        ('SRC_SWISSEPH',        N'Swiss Ephemeris / SwissEphNet', N'Astrodienst / port', N'SwissEphNet 2.8.0.2', 'astronomy', N'Moshier mode, Lahiri sidereal')
    ) v (Code, Title, Author, Edition, Tradition, Notes)
)
MERGE dbo.tbl_Dim_Source AS tgt
USING src ON tgt.Code = src.Code
WHEN MATCHED THEN UPDATE SET tgt.Title = src.Title, tgt.Author = src.Author,
    tgt.Edition = src.Edition, tgt.Tradition = src.Tradition, tgt.Notes = src.Notes
WHEN NOT MATCHED THEN INSERT (Code, Title, Author, Edition, Tradition, Notes)
    VALUES (src.Code, src.Title, src.Author, src.Edition, src.Tradition, src.Notes);
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '15_create_dim_source.sql', 'tbl_Dim_Source (SRC_* citation registry) + seed'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '15_create_dim_source.sql');
GO
DECLARE @n INT = (SELECT COUNT(*) FROM dbo.tbl_Dim_Source);
PRINT '15 applied: tbl_Dim_Source has ' + CAST(@n AS VARCHAR(10)) + ' rows.';
GO
