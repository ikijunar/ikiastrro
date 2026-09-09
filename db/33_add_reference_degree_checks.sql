-- =====================================================================
-- 33 — Reference/chart degree-domain checks
--
-- Stage 1 of the database-design review. The existing schema already enforces
-- PadaNumber, SubSequenceNumber, transit motion, and planetary longitude. This
-- migration closes the remaining degree-domain gaps without changing values.
-- =====================================================================
USE [ikiastrro];
GO

IF EXISTS (SELECT 1 FROM dbo.tbl_Nakshatras
           WHERE StartDegree < 0 OR EndDegree > 360 OR StartDegree >= EndDegree)
    THROW 50033, 'tbl_Nakshatras contains an invalid degree span.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Nakshatras_DegreeSpan')
    ALTER TABLE dbo.tbl_Nakshatras ADD CONSTRAINT CK_Nakshatras_DegreeSpan
        CHECK (StartDegree >= 0 AND EndDegree <= 360 AND StartDegree < EndDegree);
GO

IF EXISTS (SELECT 1 FROM dbo.tbl_NakshatraPadas
           WHERE StartDegree < 0 OR EndDegree > 360 OR StartDegree >= EndDegree)
    THROW 50034, 'tbl_NakshatraPadas contains an invalid degree span.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_NakshatraPadas_DegreeSpan')
    ALTER TABLE dbo.tbl_NakshatraPadas ADD CONSTRAINT CK_NakshatraPadas_DegreeSpan
        CHECK (StartDegree >= 0 AND EndDegree <= 360 AND StartDegree < EndDegree);
GO

IF EXISTS (SELECT 1 FROM dbo.tbl_NakshatraSubLords
           WHERE StartDegree < 0 OR EndDegree > 360 OR StartDegree >= EndDegree)
    THROW 50035, 'tbl_NakshatraSubLords contains an invalid degree span.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_NakshatraSubLords_DegreeSpan')
    ALTER TABLE dbo.tbl_NakshatraSubLords ADD CONSTRAINT CK_NakshatraSubLords_DegreeSpan
        CHECK (StartDegree >= 0 AND EndDegree <= 360 AND StartDegree < EndDegree);
GO

IF EXISTS (SELECT 1 FROM dbo.tbl_SignAttributes
           WHERE (ExaltedDegree IS NOT NULL AND (ExaltedDegree < 0 OR ExaltedDegree >= 30))
              OR (DebilitatedDegree IS NOT NULL AND (DebilitatedDegree < 0 OR DebilitatedDegree >= 30))
              OR (MooltrikonaRangeStart IS NOT NULL AND (MooltrikonaRangeStart < 0 OR MooltrikonaRangeStart >= 30))
              OR (MooltrikonaRangeEnd IS NOT NULL AND (MooltrikonaRangeEnd <= 0 OR MooltrikonaRangeEnd > 30))
              OR (MooltrikonaRangeStart IS NOT NULL AND MooltrikonaRangeEnd IS NOT NULL
                  AND MooltrikonaRangeStart >= MooltrikonaRangeEnd))
    THROW 50036, 'tbl_SignAttributes contains an invalid degree value or range.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_SignAttributes_DegreeRanges')
    ALTER TABLE dbo.tbl_SignAttributes ADD CONSTRAINT CK_SignAttributes_DegreeRanges
        CHECK (
            (ExaltedDegree IS NULL OR (ExaltedDegree >= 0 AND ExaltedDegree < 30)) AND
            (DebilitatedDegree IS NULL OR (DebilitatedDegree >= 0 AND DebilitatedDegree < 30)) AND
            (MooltrikonaRangeStart IS NULL OR (MooltrikonaRangeStart >= 0 AND MooltrikonaRangeStart < 30)) AND
            (MooltrikonaRangeEnd IS NULL OR (MooltrikonaRangeEnd > 0 AND MooltrikonaRangeEnd <= 30)) AND
            (MooltrikonaRangeStart IS NULL OR MooltrikonaRangeEnd IS NULL OR MooltrikonaRangeStart < MooltrikonaRangeEnd)
        );
GO

IF EXISTS (SELECT 1 FROM dbo.tbl_Chart_KeyDetails
           WHERE EclipticLatitudeDegrees IS NOT NULL
             AND (EclipticLatitudeDegrees < -90 OR EclipticLatitudeDegrees > 90))
    THROW 50037, 'tbl_Chart_KeyDetails contains an invalid ecliptic latitude.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_KeyDetails_EclipticLatitude')
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD CONSTRAINT CK_KeyDetails_EclipticLatitude
        CHECK (EclipticLatitudeDegrees IS NULL OR (EclipticLatitudeDegrees >= -90 AND EclipticLatitudeDegrees <= 90));
GO

INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '33_add_reference_degree_checks.sql', 'Degree-domain checks for reference spans, dignity ranges, and ecliptic latitude'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '33_add_reference_degree_checks.sql');
GO

PRINT '33 applied: reference and chart degree-domain checks enforced.';
GO
