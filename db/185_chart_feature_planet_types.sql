-- =====================================================================
-- 185 — vw_Chart_Feature gains the planet feature types (second step of the cross-person comparison layer).
--
-- 184 created the view with one type (YOGA). This adds four types read from tbl_Chart_KeyDetails' Graha rows
-- (Ascendant, Sun..Ketu) on every stored chart/varga, to prove the atom shape is generic:
--   PLANET_SIGN       SubjectKey = planet, ValueKey = sign            (SUN -> ARIES)
--   PLANET_HOUSE      SubjectKey = planet, ValueKey = house from Lagna (MARS -> 7); the Ascendant is skipped (always 1)
--   PLANET_NAKSHATRA  SubjectKey = planet, ValueKey = nakshatra        (MOON -> ANURADHA)
--   DIGNITY           SubjectKey = planet, ValueKey = dignity status   (VENUS -> EXALTED); absent when not stored
-- YOGA atoms are presence facts (ValueKey constant 'PRESENT'); these are valued facts, so two people share an atom
-- only when planet AND value agree. Same columns as 184, so nothing reading the view changes.
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER VIEW dbo.vw_Chart_Feature
AS
SELECT y.BirthDetailId,
       CAST(MAX(y.Name) AS nvarchar(200))            AS PersonName,
       y.ChartResultId,
       y.RuleSetId,
       y.ChartType,
       CAST('YOGA' AS varchar(20))                   AS FeatureType,
       CAST(y.YogaCode AS varchar(100))              AS SubjectKey,
       CAST('PRESENT' AS varchar(60))                AS ValueKey,
       CAST(MAX(y.YogaSetCode) AS varchar(100))      AS GroupCode,
       CAST(MAX(y.YogaSetName) AS nvarchar(200))     AS GroupName,
       CAST(MAX(y.OutcomeNatureCode) AS varchar(40)) AS NatureCode,
       CAST(MAX(y.YogaTypeCode) AS varchar(60))      AS TypeCode,
       COUNT(*)                                      AS VariantCount,
       COUNT(DISTINCT y.SourceRefCode)               AS SourceCount,
       STRING_AGG(CAST(COALESCE(y.VariantDisplayName, y.SourceVariantCode) + ' [' + y.SourceRefCode + ']' AS nvarchar(max)), '; ')
           WITHIN GROUP (ORDER BY y.SourceRefCode, y.SourceVariantCode) AS Variants
FROM dbo.vw_ChartYogaEvaluations y
WHERE y.Present = 1
GROUP BY y.BirthDetailId, y.ChartResultId, y.RuleSetId, y.ChartType, y.YogaCode

UNION ALL

SELECT c.BirthDetailId,
       CAST(b.Name AS nvarchar(200)),
       k.ChartResultId,
       c.RuleSetId,
       c.ChartType,
       CAST(f.FeatureType AS varchar(20)),
       CAST(UPPER(k.Planet) AS varchar(100)),
       CAST(f.ValueKey AS varchar(60)),
       CAST(f.GroupCode AS varchar(100)),
       CAST(f.GroupName AS nvarchar(200)),
       CAST(NULL AS varchar(40)),
       CAST(NULL AS varchar(60)),
       1,
       1,
       CAST(NULL AS nvarchar(max))
FROM dbo.tbl_Chart_KeyDetails k
JOIN dbo.tbl_ChartResults c ON c.Id = k.ChartResultId
JOIN dbo.tbl_BirthDetails b ON b.Id = c.BirthDetailId
CROSS APPLY (VALUES
    ('PLANET_SIGN',      'PLACEMENT', N'Placement', UPPER(k.Sign)),
    ('PLANET_HOUSE',     'PLACEMENT', N'Placement', CASE WHEN k.Planet = 'Ascendant' THEN NULL ELSE CAST(k.HouseNumberFromLagna AS varchar(10)) END),
    ('PLANET_NAKSHATRA', 'PLACEMENT', N'Placement', UPPER(k.Nakshatra)),
    ('DIGNITY',          'DIGNITY',   N'Dignity',   UPPER(REPLACE(k.DignityStatus, ' ', '_')))
) f(FeatureType, GroupCode, GroupName, ValueKey)
WHERE k.PointKind = 'Graha' AND f.ValueKey IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '185_chart_feature_planet_types.sql')
    INSERT dbo.SchemaMigrations (ScriptName, AppliedAtUtc, Note)
    VALUES ('185_chart_feature_planet_types.sql', SYSUTCDATETIME(),
            'vw_Chart_Feature: PLANET_SIGN, PLANET_HOUSE, PLANET_NAKSHATRA, DIGNITY from Graha key details.');
GO
PRINT '185 applied: vw_Chart_Feature planet feature types.';
GO
