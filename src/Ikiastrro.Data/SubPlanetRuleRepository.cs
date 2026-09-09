using Dapper;
using Ikiastrro.Core.Engines.Karakas;

namespace Ikiastrro.Data;

public sealed class SubPlanetRuleRepository(SqlConnectionFactory connectionFactory)
{
    public SubPlanetRuleSet GetAll(int ruleSetId)
    {
        using var connection = connectionFactory.CreateOpenConnection();
        using var rows = connection.QueryMultiple("""
            SELECT sp.SubPlanetName AS Code, CAST(r.SequenceNo AS INT) AS SequenceNo,
                   r.Operation, inp.SubPlanetName AS InputCode, CAST(r.OffsetDegrees AS FLOAT) AS OffsetDegrees
            FROM dbo.tbl_Rule_SubPlanetSunLongitude r
            JOIN dbo.tbl_Dim_SubPlanets sp ON sp.Id=r.SubPlanetId AND sp.IsActive=1
            LEFT JOIN dbo.tbl_Dim_SubPlanets inp ON inp.Id=r.InputSubPlanetId
            WHERE r.RuleSetId=@ruleSetId AND r.IsActive=1 ORDER BY r.SequenceNo;
            SELECT sp.SubPlanetName AS Code, p.PlanetName AS RulingPlanet,
                   CAST(r.PartFraction AS FLOAT) AS PartFraction, CAST(r.DivisionCount AS INT) AS DivisionCount
            FROM dbo.tbl_Rule_SubPlanetTime r
            JOIN dbo.tbl_Dim_SubPlanets sp ON sp.Id=r.SubPlanetId AND sp.IsActive=1
            JOIN dbo.tbl_Planets p ON p.Id=r.RisesInPlanetId
            WHERE r.RuleSetId=@ruleSetId AND r.IsActive=1 ORDER BY sp.SortOrder;
            SELECT r.DayNight, CAST(r.Weekday AS INT) AS Weekday,
                   CAST(r.PartNumber AS INT) AS PartNumber, p.PlanetName AS RulingPlanet
            FROM dbo.tbl_Rule_SubPlanetPartRuler r
            LEFT JOIN dbo.tbl_Planets p ON p.Id=r.RulingPlanetId
            WHERE r.RuleSetId=@ruleSetId AND r.IsActive=1;
            """, new { ruleSetId });
        var rules = new SubPlanetRuleSet(ruleSetId, rows.Read<SubPlanetSunRule>().ToArray(),
            rows.Read<SubPlanetTimeRule>().ToArray(), rows.Read<SubPlanetPartRule>().ToArray());
        rules.Validate();
        return rules;
    }
}
