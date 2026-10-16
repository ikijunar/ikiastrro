-- =====================================================================
-- 184 — vw_Chart_Feature: the first feature type (YOGA) of the cross-person comparison layer.
--
-- docs/architecture/compatibility_similarity.md §5.1 (L3b "feature atoms"): every comparable fact about a
-- chart is one narrow row, so "compare people on X" is the same set arithmetic for any X. This is the
-- view-first step (no table, nothing to keep in sync); a materialised tbl_Chart_Feature can replace it
-- later behind the same column list when C#-computed features need it.
--
-- Shape (one row per person x chart x feature subject):
--   BirthDetailId, PersonName, ChartResultId, RuleSetId, ChartType (the varga: D1, D9 ...)
--   FeatureType   'YOGA'
--   SubjectKey    the canonical identity (YogaCode), never a source variant
--   ValueKey      'PRESENT' (only present facts are atoms; absent/pending yogas are not rows)
--   GroupCode/GroupName   the yoga set (Dim_YogaSets) for grouping in a grid
--   NatureCode, TypeCode  OutcomeNatureCode, FormationFamilyCode
--   VariantCount, SourceCount, Variants   how many catalogued entries (PVR/Raman ...) back the atom, and who
--
-- A yoga catalogued by several sources/variants is therefore ONE atom per chart (RamakrishnanP D1: 63
-- source-variant rows -> 46 atoms). Reads vw_ChartYogaEvaluations, so rule-set/source joins stay in one place.
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER VIEW dbo.vw_Chart_Feature
AS
SELECT y.BirthDetailId,
       MAX(y.Name)                                   AS PersonName,
       y.ChartResultId,
       y.RuleSetId,
       y.ChartType,
       CAST('YOGA' AS varchar(20))                   AS FeatureType,
       y.YogaCode                                    AS SubjectKey,
       CAST('PRESENT' AS varchar(20))                AS ValueKey,
       MAX(y.YogaSetCode)                            AS GroupCode,
       MAX(y.YogaSetName)                            AS GroupName,
       MAX(y.OutcomeNatureCode)                      AS NatureCode,
       MAX(y.YogaTypeCode)                           AS TypeCode,
       COUNT(*)                                      AS VariantCount,
       COUNT(DISTINCT y.SourceRefCode)               AS SourceCount,
       STRING_AGG(CAST(COALESCE(y.VariantDisplayName, y.SourceVariantCode) + ' [' + y.SourceRefCode + ']' AS nvarchar(max)), '; ')
           WITHIN GROUP (ORDER BY y.SourceRefCode, y.SourceVariantCode) AS Variants
FROM dbo.vw_ChartYogaEvaluations y
WHERE y.Present = 1
GROUP BY y.BirthDetailId, y.ChartResultId, y.RuleSetId, y.ChartType, y.YogaCode;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '184_create_chart_feature_yoga_view.sql')
    INSERT dbo.SchemaMigrations (ScriptName, AppliedAtUtc, Note)
    VALUES ('184_create_chart_feature_yoga_view.sql', SYSUTCDATETIME(),
            'vw_Chart_Feature: feature-atom view, YOGA type (one row per person x chart x YogaCode, present only).');
GO
PRINT '184 applied: vw_Chart_Feature (YOGA).';
GO
