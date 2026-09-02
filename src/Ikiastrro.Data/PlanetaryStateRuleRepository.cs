using Dapper;
using Ikiastrro.Core.Engines.PlanetaryStates;

namespace Ikiastrro.Data;

/// <summary>Loads the avastha rule/dimension layer (tbl_Rule_AgeState + tbl_Rule_WakefulnessState,
/// joined to tbl_Dim_PlanetaryState for state names) for one RuleSetId into a PlanetaryStateRuleSet
/// bundle. Same shape as CombustionRuleRepository — join the dim tables, filter by RuleSetId.</summary>
public class PlanetaryStateRuleRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public PlanetaryStateRuleRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    /// <summary>Loads the rule set flagged active in tbl_Rule_Sets (currently 'Parashari-Classical', Id 1).</summary>
    public PlanetaryStateRuleSet GetActiveRuleSet()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        var activeId = connection.QuerySingle<byte>("SELECT Id FROM dbo.tbl_Rule_Sets WHERE IsActive = 1");
        return GetRuleSet(activeId);
    }

    public PlanetaryStateRuleSet GetRuleSet(byte ruleSetId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();

        const string ageSql = """
            SELECT r.Id, r.RuleSetId, r.AvasthaStateId, s.StateName,
                   r.OddSignFromDegree, r.OddSignToDegree, r.EvenSignFromDegree, r.EvenSignToDegree, r.EffectFraction
            FROM dbo.tbl_Rule_AgeState r
            JOIN dbo.tbl_Dim_PlanetaryState s ON s.Id = r.AvasthaStateId
            WHERE r.RuleSetId = @RuleSetId
            ORDER BY s.SequenceOrder
            """;
        var ageBands = connection.Query<AgeStateRuleRow>(ageSql, new { RuleSetId = ruleSetId }).ToList();

        const string wakefulnessSql = """
            SELECT r.Id, r.RuleSetId, r.DignityStatus, r.AvasthaStateId, s.StateName
            FROM dbo.tbl_Rule_WakefulnessState r
            JOIN dbo.tbl_Dim_PlanetaryState s ON s.Id = r.AvasthaStateId
            WHERE r.RuleSetId = @RuleSetId
            """;
        var wakefulness = connection.Query<WakefulnessStateRuleRow>(wakefulnessSql, new { RuleSetId = ruleSetId })
            .ToDictionary(r => r.DignityStatus, r => r);

        return new PlanetaryStateRuleSet(ruleSetId, ageBands, wakefulness);
    }
}
