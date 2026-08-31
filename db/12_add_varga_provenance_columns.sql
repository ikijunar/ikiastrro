-- =====================================================================
-- 12 — Provenance columns + VargaLongitudeDegrees.
-- tbl_ChartResults gains numeric ayanamsha + sidereal time + the varga
-- method tag. tbl_Chart_KeyDetails gains VargaLongitudeDegrees, the
-- planet's longitude in this chart's own 360 deg space ((real * N) mod 360);
-- backfilled from an inline CASE for the 6 existing chart types, then
-- NOT NULL + CHECK. Idempotent.
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/12_add_varga_provenance_columns.sql
-- =====================================================================
USE [ikiastrro];
GO
-- tbl_ChartResults provenance ----------------------------------------
IF COL_LENGTH('dbo.tbl_ChartResults', 'VargaMethod') IS NULL
ALTER TABLE dbo.tbl_ChartResults ADD
    VargaMethod       VARCHAR(40)   NULL,
    AyanamshaDegrees  DECIMAL(9,6)  NULL,
    SiderealTimeHours DECIMAL(9,6)  NULL;
GO
-- tbl_Chart_KeyDetails.VargaLongitudeDegrees (nullable first) --------
IF COL_LENGTH('dbo.tbl_Chart_KeyDetails', 'VargaLongitudeDegrees') IS NULL
ALTER TABLE dbo.tbl_Chart_KeyDetails ADD VargaLongitudeDegrees DECIMAL(9,6) NULL;
GO
-- Backfill: (NirayanaLongitudeDegrees * N) mod 360, N by chart type.
-- NirayanaLongitudeDegrees is FLOAT and T-SQL '%' rejects float operands,
-- so use the floor form: v - FLOOR(v/360)*360  (v >= 0 for all longitudes).
UPDATE kd
   SET kd.VargaLongitudeDegrees =
       CAST(
           ( (kd.NirayanaLongitudeDegrees *
              CASE cr.ChartType
                   WHEN 'D1'  THEN 1  WHEN 'D2'  THEN 2  WHEN 'D6'  THEN 6
                   WHEN 'D9'  THEN 9  WHEN 'D10' THEN 10 WHEN 'D11' THEN 11
              END)
             - FLOOR( (kd.NirayanaLongitudeDegrees *
                       CASE cr.ChartType
                            WHEN 'D1'  THEN 1  WHEN 'D2'  THEN 2  WHEN 'D6'  THEN 6
                            WHEN 'D9'  THEN 9  WHEN 'D10' THEN 10 WHEN 'D11' THEN 11
                       END) / 360.0 ) * 360.0
           ) AS DECIMAL(9,6))
  FROM dbo.tbl_Chart_KeyDetails kd
  JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
 WHERE kd.VargaLongitudeDegrees IS NULL
   AND cr.ChartType IN ('D1','D2','D6','D9','D10','D11');
GO
-- Any leftover NULL means an unexpected chart type has KeyDetails rows.
IF EXISTS (SELECT 1 FROM dbo.tbl_Chart_KeyDetails WHERE VargaLongitudeDegrees IS NULL)
    THROW 50010, 'tbl_Chart_KeyDetails still has NULL VargaLongitudeDegrees after backfill - an unexpected ChartType has rows; extend the CASE.', 1;
GO
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tbl_Chart_KeyDetails') AND name = 'VargaLongitudeDegrees' AND is_nullable = 1)
    ALTER TABLE dbo.tbl_Chart_KeyDetails ALTER COLUMN VargaLongitudeDegrees DECIMAL(9,6) NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_KeyDetails_VargaLongitude')
    ALTER TABLE dbo.tbl_Chart_KeyDetails ADD CONSTRAINT CK_KeyDetails_VargaLongitude
        CHECK (VargaLongitudeDegrees >= 0 AND VargaLongitudeDegrees < 360);
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '12_add_varga_provenance_columns.sql', 'ChartResults provenance cols + KeyDetails.VargaLongitudeDegrees NOT NULL + CHECK'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '12_add_varga_provenance_columns.sql');
GO
PRINT '12 applied: provenance + VargaLongitudeDegrees.';
GO
