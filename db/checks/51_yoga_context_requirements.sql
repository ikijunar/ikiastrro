-- Read-only inspection for migration 51.
SET NOCOUNT ON;

SELECT RuleSetId, SourceRefCode, SourceVariantCode, RequirementCode,
       DerivationMethodCode, MissingBehavior, Notes
FROM dbo.vw_YogaContextRequirements
WHERE IsActive = 1
ORDER BY SourceRefCode, SourceVariantCode, RequirementCode;

SELECT RequirementCode, COUNT(*) AS VariantRequirementCount
FROM dbo.vw_YogaContextRequirements
WHERE IsActive = 1
GROUP BY RequirementCode
ORDER BY RequirementCode;

SELECT SourceRefCode, SourceVariantCode, RequirementCode
FROM dbo.vw_YogaContextRequirements
WHERE IsActive = 1
  AND MissingBehavior <> 'NOT_EVALUATED';
