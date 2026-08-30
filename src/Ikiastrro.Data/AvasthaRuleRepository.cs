using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>Loads the avastha rule/dimension layer (tbl_Rule_BaaladiState + tbl_Rule_JagradadiState,
/// joined to tbl_Dim_AvasthaState for state names) for one RuleSetId into an AvasthaRuleSet bundle.
/// Same shape as CombustionRuleRepository — join the dim tables, filter by RuleSetId.</summary>
public class AvasthaRuleRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AvasthaRuleRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    /// <summary>Loads the rule set flagged active in tbl_Rule_Sets (currently 'Parashari-Classical', Id 1).</summary>
    public AvasthaRuleSet GetActiveRuleSet()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        var activeId = connection.QuerySingle<byte>("SELECT Id FROM dbo.tbl_Rule_Sets WHERE IsActive = 1");
        return GetRuleSet(activeId);
    }

    public AvasthaRuleSet GetRuleSet(byte ruleSetId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();

        const string baaladiSql = """
            SELECT r.Id, r.RuleSetId, r.AvasthaStateId, s.StateName,
                   r.OddSignFromDegree, r.OddSignToDegree, r.EvenSignFromDegree, r.EvenSignToDegree, r.EffectFraction
            FROM dbo.tbl_Rule_BaaladiState r
            JOIN dbo.tbl_Dim_AvasthaState s ON s.Id = r.AvasthaStateId
            WHERE r.RuleSetId = @RuleSetId
            ORDER BY s.SequenceOrder
            """;
        var baaladi = connection.Query<BaaladiRuleRow>(baaladiSql, new { RuleSetId = ruleSetId }).ToList();

        const string jagradadiSql = """
            SELECT r.Id, r.RuleSetId, r.DignityStatus, r.AvasthaStateId, s.StateName
            FROM dbo.tbl_Rule_JagradadiState r
            JOIN dbo.tbl_Dim_AvasthaState s ON s.Id = r.AvasthaStateId
            WHERE r.RuleSetId = @RuleSetId
            """;
        var jagradadi = connection.Query<JagradadiRuleRow>(jagradadiSql, new { RuleSetId = ruleSetId })
            .ToDictionary(r => r.DignityStatus, r => r);

        return new AvasthaRuleSet(ruleSetId, baaladi, jagradadi);
    }
}
