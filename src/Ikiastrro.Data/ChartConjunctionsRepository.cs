using Dapper;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Data;

/// <summary>tbl_Chart_Conjunctions — shared across every chart type (D1, D9, and any future divisional chart), rows discriminated by ChartResultId/ChartType.</summary>
public class ChartConjunctionsRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ChartConjunctionsRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void InsertAll(IEnumerable<ChartConjunction> rows)
    {
        const string sql = """
            INSERT INTO dbo.tbl_Chart_Conjunctions
                (ChartResultId, BirthDetailId, ChartType, Planet1, Planet2, Sign, HouseNumberFromLagna, DegreeSeparation, ComputedAt)
            VALUES
                (@ChartResultId, @BirthDetailId, @ChartType, @Planet1, @Planet2, @Sign, @HouseNumberFromLagna, @DegreeSeparation, @ComputedAt)
            """;

        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, rows);
    }

    public IReadOnlyList<ChartConjunction> GetByChartResultId(int chartResultId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Chart_Conjunctions WHERE ChartResultId = @ChartResultId ORDER BY Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<ChartConjunction>(sql, new { ChartResultId = chartResultId }).ToList();
    }

    /// <summary>Every Conjunction row for one person, all chart types — for the Web workspace's one-shot load.</summary>
    public IReadOnlyList<ChartConjunction> GetByBirthDetailId(int birthDetailId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Chart_Conjunctions WHERE BirthDetailId = @BirthDetailId ORDER BY Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<ChartConjunction>(sql, new { BirthDetailId = birthDetailId }).ToList();
    }

    /// <summary>Deletes every row (every chart type) for one person — used by BirthDetailDeletionService.</summary>
    public void DeleteByBirthDetailId(int birthDetailId)
    {
        const string sql = "DELETE FROM dbo.tbl_Chart_Conjunctions WHERE BirthDetailId = @BirthDetailId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { BirthDetailId = birthDetailId });
    }

    /// <summary>Deletes just one ChartResult's rows (chart types share this table, discriminated by ChartResultId) — used by ChartGenerationService.RecomputeAnalytics to re-derive rows for a ChartResult that already has them.</summary>
    public void DeleteByChartResultId(int chartResultId)
    {
        const string sql = "DELETE FROM dbo.tbl_Chart_Conjunctions WHERE ChartResultId = @ChartResultId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { ChartResultId = chartResultId });
    }
}
