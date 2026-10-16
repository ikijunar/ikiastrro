-- =====================================================================
-- 186 — vw_Feature_BaseRate: how common each feature atom is among the saved people.
--
-- docs/architecture/compatibility_similarity.md §5.3. Sharing a common atom (47 of 70 people have Daridra) says
-- little; sharing a rare one says more. This view gives the frequency of every (FeatureType, ChartType, SubjectKey,
-- ValueKey) in the SAVED population, so the compare screens can show "x of y people" beside a shared fact.
--
-- It is a rate over saved people, NOT a population baseline: the planned tbl_Feature_BaseRate (many generated birth
-- times) is still open. Population is the number of people who have at least one atom of that feature type on that
-- chart, so a varga with partial coverage is not diluted by people it was never computed for.
--
-- A view over vw_Chart_Feature, so nothing to refresh; a materialised table is only worth it if this gets slow.
-- =====================================================================
USE [ikiastrro];
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER VIEW dbo.vw_Feature_BaseRate
AS
WITH atoms AS (
    SELECT DISTINCT BirthDetailId, FeatureType, ChartType, SubjectKey, ValueKey
    FROM dbo.vw_Chart_Feature
),
pop AS (
    SELECT FeatureType, ChartType, COUNT(DISTINCT BirthDetailId) AS Population
    FROM atoms
    GROUP BY FeatureType, ChartType
)
SELECT a.FeatureType, a.ChartType, a.SubjectKey, a.ValueKey,
       COUNT(*)                                              AS People,
       p.Population,
       CAST(COUNT(*) * 1.0 / p.Population AS decimal(9, 6)) AS Rate
FROM atoms a
JOIN pop p ON p.FeatureType = a.FeatureType AND p.ChartType = a.ChartType
GROUP BY a.FeatureType, a.ChartType, a.SubjectKey, a.ValueKey, p.Population;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = '186_create_feature_base_rate_view.sql')
    INSERT dbo.SchemaMigrations (ScriptName, AppliedAtUtc, Note)
    VALUES ('186_create_feature_base_rate_view.sql', SYSUTCDATETIME(),
            'vw_Feature_BaseRate: frequency of each feature atom among saved people (not a population baseline).');
GO
PRINT '186 applied: vw_Feature_BaseRate.';
GO
