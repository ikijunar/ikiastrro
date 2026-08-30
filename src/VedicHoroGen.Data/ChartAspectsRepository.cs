using Dapper;
using VedicHoroGen.Core.Models;

namespace VedicHoroGen.Data;

/// <summary>tbl_Chart_Aspects — shared across every chart type (D1, D9, and any future divisional chart), rows discriminated by ChartResultId/ChartType.</summary>
public class ChartAspectsRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ChartAspectsRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void InsertAll(IEnumerable<ChartAspect> rows)
    {
        const string sql = """
            INSERT INTO dbo.tbl_Chart_Aspects
                (ChartResultId, BirthDetailId, ChartType, AspectingPlanet, AspectedTarget, AspectType, ComputedAt)
            VALUES
                (@ChartResultId, @BirthDetailId, @ChartType, @AspectingPlanet, @AspectedTarget, @AspectType, @ComputedAt)
            """;

        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, rows);
    }

    public IReadOnlyList<ChartAspect> GetByChartResultId(int chartResultId)
    {
        const string sql = "SELECT * FROM dbo.tbl_Chart_Aspects WHERE ChartResultId = @ChartResultId ORDER BY Id";
        using var connection = _connectionFactory.CreateOpenConnection();
        return connection.Query<ChartAspect>(sql, new { ChartResultId = chartResultId }).ToList();
    }

    /// <summary>Deletes every row (every chart type) for one person — used by BirthDetailDeletionService.</summary>
    public void DeleteByBirthDetailId(int birthDetailId)
    {
        const string sql = "DELETE FROM dbo.tbl_Chart_Aspects WHERE BirthDetailId = @BirthDetailId";
        using var connection = _connectionFactory.CreateOpenConnection();
        connection.Execute(sql, new { BirthDetailId = birthDetailId });
    }
}
