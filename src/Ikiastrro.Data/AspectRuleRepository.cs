using Dapper;
using Ikiastrro.Core.Astro;

namespace Ikiastrro.Data;

/// <summary>
/// tbl_Rule_AspectOffset -- returns the same shape ClassicalRelationships.AspectOffsets
/// hardcodes today, so a future Phase 2 wiring swap is a one-line change at the call site.
/// </summary>
public class AspectRuleRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AspectRuleRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IReadOnlyDictionary<PlanetName, int[]> GetOffsets(int ruleSetId)
    {
        const string sql = """
            SELECT p.PlanetName, r.HouseOffset
            FROM dbo.tbl_Rule_AspectOffset r
            JOIN dbo.tbl_Planets p ON p.Id = r.PlanetId
            WHERE r.RuleSetId = @RuleSetId
            ORDER BY p.PlanetName, r.HouseOffset
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        var rows = connection.Query<(string PlanetName, int HouseOffset)>(sql, new { RuleSetId = ruleSetId });
        return rows
            .GroupBy(r => Enum.Parse<PlanetName>(r.PlanetName))
            .ToDictionary(g => g.Key, g => g.Select(r => r.HouseOffset).ToArray());
    }
}
