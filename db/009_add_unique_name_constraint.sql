-- vedic_horo_gen: enforce "no duplicate Name" on tbl_BirthDetails.
-- Application code (BirthDetailsRepository.ExistsByName, used by both the CLI and the
-- Blazor web front end) checks this first to give a friendly message; this unique index
-- is the DB-level backstop against races/direct inserts.
--
-- Uses the column's default collation (SQL_Latin1_General_CP1_CI_AS on this instance),
-- which is case-insensitive, so 'Raj Kumar' and 'raj kumar' collide as expected.
USE vedic_horo_gen;
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'UX_BirthDetails_Name' AND object_id = OBJECT_ID('dbo.tbl_BirthDetails')
)
BEGIN
    CREATE UNIQUE INDEX UX_BirthDetails_Name ON dbo.tbl_BirthDetails (Name);
END
GO
