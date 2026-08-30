-- 029_drop_jhd_export_table.sql
-- Drops tbl_Chart_JhdExport (migration 016) plus its two dependents, per rammyps's explicit
-- request. Confirmed safe before dropping: 5 rows, zero IsOverridden=1 (no hand-edited data
-- lost), zero FK references from any other table, zero C# code references anywhere in the
-- solution (it was DB-level only -- "no CLI file-writer yet" per its original build note in
-- cproj_vedic_horo_gen.md, 2026-08-28 -- never got wired into the app).

IF OBJECT_ID('vw_Chart_JhdExport', 'V') IS NOT NULL
    DROP VIEW vw_Chart_JhdExport;
GO

IF OBJECT_ID('usp_Chart_RecomputeJhdExport', 'P') IS NOT NULL
    DROP PROCEDURE usp_Chart_RecomputeJhdExport;
GO

IF OBJECT_ID('tbl_Chart_JhdExport', 'U') IS NOT NULL
    DROP TABLE tbl_Chart_JhdExport;
GO
