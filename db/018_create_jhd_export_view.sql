-- vedic_horo_gen: JhdExport view — read-only, human-readable presentation of
-- tbl_Chart_JhdExport with columns in exact .jhd file line order (Month/Day/Year/Time/
-- TimeZone/Longitude/Latitude/Reserved1/LmtRef1/LmtRef2/Reserved2/AtlasId/PlaceCity/
-- PlaceCountry/Flag), joined to tbl_BirthDetails so a row can be identified by Name
-- instead of BirthDetailId. One row per person; tbl_Chart_JhdExport itself stays the
-- source of truth (and the only place IsOverridden can be set) — this view never writes.
USE vedic_horo_gen;
GO

CREATE OR ALTER VIEW dbo.vw_Chart_JhdExport AS
SELECT
    bd.Id            AS BirthDetailId,
    bd.Name,
    je.Month,          -- line 1
    je.Day,            -- line 2
    je.Year,           -- line 3
    je.Time,           -- line 4
    je.TimeZone,       -- line 5
    je.Longitude,      -- line 6
    je.Latitude,       -- line 7
    je.Reserved1,      -- line 8
    je.LmtRef1,        -- line 9
    je.LmtRef2,        -- line 10
    je.Reserved2,      -- line 11
    je.AtlasId,        -- line 12
    je.PlaceCity,      -- line 13
    je.PlaceCountry,   -- line 14
    je.Flag,           -- line 15
    je.IsOverridden,
    je.ComputedAt
FROM dbo.tbl_Chart_JhdExport je
JOIN dbo.tbl_BirthDetails bd ON bd.Id = je.BirthDetailId;
GO
