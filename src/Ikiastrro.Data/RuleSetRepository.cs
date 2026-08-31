using Dapper;

namespace Ikiastrro.Data;

public record RuleSet(
    int Id, string RuleSetName, string? Description,
    int VersionNumber, DateTime EffectiveFromUtc, DateTime? EffectiveToUtc, bool IsPublished);

/// <summary>tbl_Rule_Sets -- the version dimension every other tbl_Rule_* table hangs off.</summary>
public class RuleSetRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public RuleSetRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // Id is TINYINT (maps to byte) but the RuleSet record uses int for idiomatic C# -- Dapper's
    // constructor-matching for records requires an exact type match with no parameterless
    // constructor available, so cast in SQL rather than declare Id as byte in the public API.
    private const string SelectColumns = "CAST(Id AS INT) AS Id, RuleSetName, Description, VersionNumber, EffectiveFromUtc, EffectiveToUtc, IsPublished";

    /// <summary>The one rule set flagged IsActive=1 -- what calculators use unless told otherwise.</summary>
    public RuleSet GetActive()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.QuerySingle<RuleSet>(
            $"SELECT {SelectColumns} FROM dbo.tbl_Rule_Sets WHERE IsActive = 1");
    }

    public IReadOnlyList<RuleSet> GetAll()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<RuleSet>($"SELECT {SelectColumns} FROM dbo.tbl_Rule_Sets ORDER BY Id").ToList();
    }
}
