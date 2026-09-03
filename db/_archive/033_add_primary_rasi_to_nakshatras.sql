-- 033_add_primary_rasi_to_nakshatras.sql
-- Gives tbl_Nakshatras its own sign columns (previously only inferable via its 4 pada rows).
-- PrimaryRasiId = sign of the nakshatra's midpoint. StraddlesSignBoundary = 1 when the first and
-- last pada fall in different signs (the 9 boundary-spanning nakshatras: Krittika, Mrigashira,
-- Punarvasu, Uttara Phalguni, Chitra, Vishakha, Uttara Ashadha, Dhanishta, Purva Bhadrapada).

IF COL_LENGTH('dbo.tbl_Nakshatras', 'PrimaryRasiId') IS NULL
    ALTER TABLE dbo.tbl_Nakshatras ADD PrimaryRasiId TINYINT NULL;
GO
IF COL_LENGTH('dbo.tbl_Nakshatras', 'StraddlesSignBoundary') IS NULL
    ALTER TABLE dbo.tbl_Nakshatras ADD StraddlesSignBoundary BIT NOT NULL CONSTRAINT DF_Nakshatras_Straddles DEFAULT 0;
GO

UPDATE n
SET PrimaryRasiId = FLOOR(((n.StartDegree + n.EndDegree) / 2.0) / 30.0) + 1
FROM dbo.tbl_Nakshatras n;
GO

UPDATE n
SET StraddlesSignBoundary = CASE WHEN p1.RasiId <> p4.RasiId THEN 1 ELSE 0 END
FROM dbo.tbl_Nakshatras n
JOIN dbo.tbl_NakshatraPadas p1 ON p1.NakshatraId = n.Id AND p1.PadaNumber = 1
JOIN dbo.tbl_NakshatraPadas p4 ON p4.NakshatraId = n.Id AND p4.PadaNumber = 4;
GO

ALTER TABLE dbo.tbl_Nakshatras ALTER COLUMN PrimaryRasiId TINYINT NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Nakshatras_PrimaryRasi')
    ALTER TABLE dbo.tbl_Nakshatras
        ADD CONSTRAINT FK_Nakshatras_PrimaryRasi
        FOREIGN KEY (PrimaryRasiId) REFERENCES dbo.tbl_SignAttributes(Id);
GO
