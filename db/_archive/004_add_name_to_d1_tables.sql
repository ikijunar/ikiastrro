-- vedic_horo_gen: add denormalized Name to both D1 detail tables, so they're queryable/readable
-- standalone without a join back to BirthDetails every time.
USE vedic_horo_gen;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_D1Chart_keydetails') AND name = 'Name')
BEGIN
    ALTER TABLE dbo.tbl_D1Chart_keydetails ADD Name NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_D1Chart_HouseLords') AND name = 'Name')
BEGIN
    ALTER TABLE dbo.tbl_D1Chart_HouseLords ADD Name NVARCHAR(200) NULL;
END
GO

-- Backfill existing rows from BirthDetails
UPDATE kd
SET kd.Name = bd.Name
FROM dbo.tbl_D1Chart_keydetails kd
JOIN dbo.tbl_BirthDetails bd ON bd.Id = kd.BirthDetailId
WHERE kd.Name IS NULL;
GO

UPDATE hl
SET hl.Name = bd.Name
FROM dbo.tbl_D1Chart_HouseLords hl
JOIN dbo.tbl_BirthDetails bd ON bd.Id = hl.BirthDetailId
WHERE hl.Name IS NULL;
GO
