using Dapper;
using Ikiastrro.Core.Engines.PlanetaryStates;

namespace Ikiastrro.Data;

/// <summary>tbl_Fact_PlanetaryState — computed avastha states per planet per chart, shared across
/// every chart type (rows discriminated by ChartResultId / ChartType). Mirrors
/// ChartKeyDetailsRepository's insert / delete / get-by-* shape.</summary>
public class PlanetaryStateRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public PlanetaryStateRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public void InsertAll(IEnumerable<PlanetaryStateFact> rows)
    {
        const string sql = """
            INSERT INTO dbo.tbl_Fact_PlanetaryState
                (ChartResultId, Planet, RuleSetId,
                 AgeStateId, AgeEffectFraction, WakefulnessStateId,
                 PlanetId)
            VALUES
                (@ChartResultId, @Planet, @RuleSetId,
                 @AgeStateId, @AgeEffectFraction, @WakefulnessStateId,
                 @PlanetId)
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, rows);
    }

    public IReadOnlyList<PlanetaryStateFact> GetByChartResultId(int chartResultId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Fact_PlanetaryState WHERE ChartResultId = @ChartResultId ORDER BY Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<PlanetaryStateFact>(sql, new { ChartResultId = chartResultId }).ToList();
    }

    /// <summary>Every avastha row for one person, all chart types — for the Web workspace's one-shot load.</summary>
    public IReadOnlyList<PlanetaryStateFact> GetByBirthDetailId(int birthDetailId)
    {
        const string sql = """
            SELECT * FROM dbo.tbl_Fact_PlanetaryState
            WHERE ChartResultId IN (SELECT Id FROM dbo.tbl_ChartResults WHERE BirthDetailId = @BirthDetailId)
            ORDER BY Id
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<PlanetaryStateFact>(sql, new { BirthDetailId = birthDetailId }).ToList();
    }

    /// <summary>Deletes every row (every chart type) for one person — used by BirthDetailDeletionService / GenerateAll.</summary>
    public void DeleteByBirthDetailId(int birthDetailId)
    {
        const string sql = """
            DELETE FROM dbo.tbl_Fact_PlanetaryState
            WHERE ChartResultId IN (SELECT Id FROM dbo.tbl_ChartResults WHERE BirthDetailId = @BirthDetailId)
            """;
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { BirthDetailId = birthDetailId });
    }

    /// <summary>Deletes just one ChartResult's rows — used by ChartGenerationService.RecomputeAnalytics.</summary>
    public void DeleteByChartResultId(int chartResultId)
    {
        const string sql = "DELETE FROM dbo.tbl_Fact_PlanetaryState WHERE ChartResultId = @ChartResultId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { ChartResultId = chartResultId });
    }
}
