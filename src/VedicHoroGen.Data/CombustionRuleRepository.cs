using Dapper;
using VedicHoroGen.Core.Astro;

namespace VedicHoroGen.Data;

/// <summary>tbl_Rule_CombustionOrb -- same two-dictionary shape ClassicalCombustion hardcodes today.</summary>
public class CombustionRuleRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public CombustionRuleRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IReadOnlyDictionary<PlanetName, (decimal Direct, decimal? Retrograde)> GetOrbs(int ruleSetId)
    {
        const string sql = """
            SELECT p.PlanetName, r.DirectOrbDegrees, r.RetrogradeOrbDegrees
            FROM dbo.tbl_Rule_CombustionOrb r
            JOIN dbo.tbl_Planets p ON p.Id = r.PlanetId
            WHERE r.RuleSetId = @RuleSetId
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        var rows = connection.Query<(string PlanetName, decimal DirectOrbDegrees, decimal? RetrogradeOrbDegrees)>(sql, new { RuleSetId = ruleSetId });
        return rows.ToDictionary(
            r => Enum.Parse<PlanetName>(r.PlanetName),
            r => (r.DirectOrbDegrees, r.RetrogradeOrbDegrees));
    }
}
