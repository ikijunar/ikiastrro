-- =====================================================================
-- 13 — Un-gate DegreesInSignDecimal / DegreesInSignDisplay from D1-only.
-- For every existing NON-D1 KeyDetails row, set them from
-- VargaLongitudeDegrees % 30 (PyJHora's d_long). D1 rows are left as-is.
-- The display string replicates AstroMath.FormatDegreesMinutesSeconds
-- exactly: totalSeconds = round(deg*3600); "{d}°{m}'{s}\"" with NO zero
-- padding. Going forward ChartAnalyzer writes these directly (Plan-A
-- Task 14); Task 16's recompute-keydetails regenerates every row anyway.
-- Idempotent (re-run recomputes to the same values).
-- Apply:  sqlcmd -S localhost -E -d ikiastrro -i db/13_ungate_degrees_in_sign.sql
-- =====================================================================
USE [ikiastrro];
GO
;WITH v AS (
    SELECT kd.Id,
           CAST(kd.VargaLongitudeDegrees % 30 AS DECIMAL(7,4)) AS deg,
           CAST(ROUND((kd.VargaLongitudeDegrees % 30) * 3600.0, 0) AS BIGINT) AS totsec
    FROM dbo.tbl_Chart_KeyDetails kd
    JOIN dbo.tbl_ChartResults cr ON cr.Id = kd.ChartResultId
    WHERE cr.ChartType <> 'D1'
)
-- NCHAR() codepoints (not literal glyphs) so the script is encoding-safe under
-- sqlcmd -i: 176 = degree sign, 39 = apostrophe (minutes), 34 = quote (seconds).
UPDATE kd
   SET kd.DegreesInSignDecimal = v.deg,
       kd.DegreesInSignDisplay =
           CAST(v.totsec / 3600 AS VARCHAR(3)) + NCHAR(176) +
           CAST((v.totsec % 3600) / 60 AS VARCHAR(2)) + NCHAR(39) +
           CAST(v.totsec % 60 AS VARCHAR(2)) + NCHAR(34)
  FROM dbo.tbl_Chart_KeyDetails kd
  JOIN v ON v.Id = kd.Id;
GO
INSERT dbo.SchemaMigrations (ScriptName, Note)
SELECT '13_ungate_degrees_in_sign.sql', 'DegreesInSignDecimal/Display populated for existing non-D1 rows from VargaLongitudeDegrees'
WHERE NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '13_ungate_degrees_in_sign.sql');
GO
PRINT '13 applied: degrees-in-sign un-gated for existing varga rows.';
GO
