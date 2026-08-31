using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>tbl_Fact_PlanetAvastha — computed avastha states per planet per chart, shared across
/// every chart type (rows discriminated by ChartResultId / ChartType). Mirrors
/// ChartKeyDetailsRepository's insert / delete / get-by-* shape.</summary>
public class PlanetAvasthaRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public PlanetAvasthaRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public void InsertAll(IEnumerable<PlanetAvasthaFact> rows)
    {
        const string sql = """
            INSERT INTO dbo.tbl_Fact_PlanetAvastha
                (ChartResultId, BirthDetailId, ChartType, Planet, RuleSetId,
                 BaaladiStateId, BaaladiEffectFraction, JagradadiStateId, ComputedAt,
                 PlanetId)
            VALUES
                (@ChartResultId, @BirthDetailId, @ChartType, @Planet, @RuleSetId,
                 @BaaladiStateId, @BaaladiEffectFraction, @JagradadiStateId, @ComputedAt,
                 @PlanetId)
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, rows);
    }

    public IReadOnlyList<PlanetAvasthaFact> GetByChartResultId(int chartResultId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Fact_PlanetAvastha WHERE ChartResultId = @ChartResultId ORDER BY Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<PlanetAvasthaFact>(sql, new { ChartResultId = chartResultId }).ToList();
    }

    /// <summary>Every avastha row for one person, all chart types — for the Web workspace's one-shot load.</summary>
    public IReadOnlyList<PlanetAvasthaFact> GetByBirthDetailId(int birthDetailId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Fact_PlanetAvastha WHERE BirthDetailId = @BirthDetailId ORDER BY Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<PlanetAvasthaFact>(sql, new { BirthDetailId = birthDetailId }).ToList();
    }

    /// <summary>Deletes every row (every chart type) for one person — used by BirthDetailDeletionService / GenerateAll.</summary>
    public void DeleteByBirthDetailId(int birthDetailId)
    {
        const string sql = "DELETE FROM dbo.tbl_Fact_PlanetAvastha WHERE BirthDetailId = @BirthDetailId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { BirthDetailId = birthDetailId });
    }

    /// <summary>Deletes just one ChartResult's rows — used by ChartGenerationService.RecomputeAnalytics.</summary>
    public void DeleteByChartResultId(int chartResultId)
    {
        const string sql = "DELETE FROM dbo.tbl_Fact_PlanetAvastha WHERE ChartResultId = @ChartResultId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { ChartResultId = chartResultId });
    }
}
