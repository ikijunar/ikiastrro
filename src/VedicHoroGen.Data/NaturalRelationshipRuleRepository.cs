using Dapper;
using VedicHoroGen.Core.Astro;

namespace VedicHoroGen.Data;

/// <summary>
/// tbl_Rule_NaturalRelationship -- same (Friends,Neutrals,Enemies) tuple shape
/// ClassicalDignity.NaturalRelationship hardcodes today.
/// </summary>
public class NaturalRelationshipRuleRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public NaturalRelationshipRuleRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IReadOnlyDictionary<PlanetName, (PlanetName[] Friends, PlanetName[] Neutrals, PlanetName[] Enemies)> GetRelationships(int ruleSetId)
    {
        const string sql = """
            SELECT p.PlanetName, rp.PlanetName AS RelatedPlanetName, r.RelationshipType
            FROM dbo.tbl_Rule_NaturalRelationship r
            JOIN dbo.tbl_Planets p ON p.Id = r.PlanetId
            JOIN dbo.tbl_Planets rp ON rp.Id = r.RelatedPlanetId
            WHERE r.RuleSetId = @RuleSetId
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        var rows = connection.Query<(string PlanetName, string RelatedPlanetName, string RelationshipType)>(sql, new { RuleSetId = ruleSetId });

        return rows
            .GroupBy(r => Enum.Parse<PlanetName>(r.PlanetName))
            .ToDictionary(g => g.Key, g => (
                Friends: g.Where(r => r.RelationshipType == "Friend").Select(r => Enum.Parse<PlanetName>(r.RelatedPlanetName)).ToArray(),
                Neutrals: g.Where(r => r.RelationshipType == "Neutral").Select(r => Enum.Parse<PlanetName>(r.RelatedPlanetName)).ToArray(),
                Enemies: g.Where(r => r.RelationshipType == "Enemy").Select(r => Enum.Parse<PlanetName>(r.RelatedPlanetName)).ToArray()
            ));
    }
}
