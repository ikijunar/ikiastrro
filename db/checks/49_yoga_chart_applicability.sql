USE [ikiastrro];
GO

SELECT SourceRefCode, SourceVariantCode, ChartCode, RequirementRole,
       EvaluationScope, MissingBehavior
FROM dbo.vw_YogaChartApplicability
WHERE IsActive = 1
ORDER BY SourceRefCode, SourceVariantCode,
         CASE RequirementRole WHEN 'FOUNDATION' THEN 1 WHEN 'REQUIRED' THEN 2 ELSE 3 END,
         ChartCode;

SELECT SourceRefCode, SourceVariantCode,
       STRING_AGG(ChartCode + ':' + RequirementRole, ', ')
           WITHIN GROUP (ORDER BY ChartCode) AS RequiredCharts
FROM dbo.vw_YogaChartApplicability
WHERE IsActive = 1
GROUP BY SourceRefCode, SourceVariantCode
ORDER BY SourceRefCode, SourceVariantCode;
GO
